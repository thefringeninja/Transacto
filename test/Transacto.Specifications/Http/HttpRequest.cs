using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Net.Http.Headers;

namespace Transacto.Http;

public record HttpRequest(HttpMethod Method, string Url, RequestHeaders? Headers = null, byte[]? Body = null) {
	public static implicit operator HttpRequestMessage(HttpRequest request) {
		var httpRequestMessage = new HttpRequestMessage(request.Method, request.Url);

		if (request.Body is not null) {
			httpRequestMessage.Content = new ByteArrayContent(request.Body);
		}

		if (request.Headers is not null) {
			foreach (var (name, values) in request.Headers.Headers) {
				httpRequestMessage.Headers.TryAddWithoutValidation(name, values.ToArray());
				httpRequestMessage.Content?.Headers.TryAddWithoutValidation(name, values.ToArray());
			}
		}

		return httpRequestMessage;
	}

	public static HttpRequest GetHalJson(string url) => new(HttpMethod.Get, url,
		new RequestHeaders(new HeaderDictionary()) {
			Accept = [new("application/hal+json")]
		});

	public static HttpRequest Command(object command, string url, JsonSerializerOptions serializerOptions) {
		var content = new MultipartFormDataContent {
			{ new StringContent(command.GetType().Name), nameof(command) }, {
				new ReadOnlyMemoryContent(
					JsonSerializer.SerializeToUtf8Bytes(command, serializerOptions)),
				"data", "data"
			}
		};
		var body = content.ReadAsByteArrayAsync().Result;

		var contentHeaders = new HeaderDictionary();
		foreach (var (name, values) in content.Headers) {
			contentHeaders.Add(name, values.ToArray());
		}

		return new HttpRequest(HttpMethod.Post, url, new RequestHeaders(contentHeaders), body);
	}

	public bool IsJson { get; } = Headers is { ContentType: { } contentType } &&
	                              (contentType.SubType == "json" || contentType.Suffix == "json");

	public bool IsText { get; } = Headers is { ContentType: { } contentType } && contentType.IsSubsetOf(s_text);

	private static readonly MediaTypeHeaderValue s_text = new("text/*");


	public override string ToString() {
		var builder = new StringBuilder(Method.Method).Append(' ').Append(Url).Append(' ').Append("HTTP/1.1")
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
