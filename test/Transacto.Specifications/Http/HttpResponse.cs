using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Hallo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Transacto.Framework.Http;

namespace Transacto.Http;

public record HttpResponse(
	HttpStatusCode StatusCode,
	string? ReasonPhrase = null,
	ResponseHeaders? Headers = null,
	byte[]? Body = null) {
	public static HttpResponse HalJson<T>(T resource, IHal hal) {
		var body = JsonSerializer.SerializeToUtf8Bytes(hal.RepresentationOfAsync(resource).Result,
			TransactoHttpSerializerOptions.HalJsonSerializerOptions);

		return new HttpResponse(HttpStatusCode.OK, "OK", new ResponseHeaders(new HeaderDictionary()) {
			ContentType = new MediaTypeHeaderValue("application/hal+json")
		}, body);
	}

	public static implicit operator HttpResponse(HttpResponseMessage httpResponseMessage) {
		var headers = new HeaderDictionary(httpResponseMessage.Headers.Concat(httpResponseMessage.Content.Headers)
			.ToDictionary(header => header.Key, header => new StringValues(header.Value.ToArray())));

		return new HttpResponse(httpResponseMessage.StatusCode,
			httpResponseMessage.ReasonPhrase, new ResponseHeaders(headers),
			httpResponseMessage.Content.ReadAsByteArrayAsync().Result);
	}

	public override int GetHashCode() {
		return base.GetHashCode();
	}

	public virtual bool Equals(HttpResponse? other) {
		if (other is null) {
			return false;
		}

		if (StatusCode != other.StatusCode) {
			return false;
		}

		if (!string.Equals(ReasonPhrase, other.ReasonPhrase)) {
			return false;
		}

		if (IsJson && other.IsJson) {
			return JsonEquals(this, other);
		}

		return Body.SequenceEqual(other.Body);
	}

	public bool IsJson { get; } = Headers is { ContentType: { } contentType } &&
	                              (contentType.SubType == "json" || contentType.Suffix == "json");

	public bool IsText { get; } = Headers is { ContentType: { } contentType } && contentType.IsSubsetOf(s_text);

	private static readonly MediaTypeHeaderValue s_text = new("text/*");

	private static bool JsonEquals(HttpResponse left, HttpResponse right) =>
		JsonNode.DeepEquals(JsonNode.Parse(left.Body), JsonNode.Parse(right.Body));

	public override string ToString() {
		var builder = new StringBuilder("HTTP/1.1 ").Append((int)StatusCode).Append(' ').Append(ReasonPhrase)
			.AppendLine();

		if (Headers is not null) {
			builder = Headers.Headers.Aggregate(
				builder,
				(acc, current) => acc.Append(current.Key).Append(": ")
					.Append(string.Join(", ", current.Value.ToArray())).AppendLine());
		}

		builder = builder.AppendLine();

		if (IsJson) {
			return builder
				.AppendLine(JsonNode.Parse(Body)?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }))
				.ToString();
		}

		if (IsText) {
			return builder.AppendLine(Encoding.UTF8.GetString(Body ?? [])).ToString();
		}

		if (Body?.Length > 0) {
			return builder.AppendLine("[binary]").ToString();
		}

		return builder.AppendLine().ToString();
	}
}
