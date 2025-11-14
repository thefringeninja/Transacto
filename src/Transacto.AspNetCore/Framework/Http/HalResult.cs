using System.Net;
using System.Text.Json;
using Hallo;
using Hallo.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace Transacto.Framework.Http;

internal class HalResult<T>(T? resource, Checkpoint checkpoint, IHal hal, HttpStatusCode statusCode = HttpStatusCode.OK)
	: IResult {
	private const string HalJsonContentType = "application/hal+json";
	private const string HalHtmlContentType = "text/html";
	private static readonly MediaType HalJson = new(HalJsonContentType);
	private static readonly MediaType Html = new(HalHtmlContentType);

	public Task ExecuteAsync(HttpContext context) {
		context.Response.StatusCode = (int)statusCode;

		var requestHeaders = context.Request.GetTypedHeaders();

		if (checkpoint != default) {
			for (var i = 0; i < requestHeaders.IfMatch.Count; i++) {
				var etag = requestHeaders.IfMatch[i].Tag;
				if (!etag.HasValue) {
					continue;
				}

				var requestedCheckpoint = Checkpoint.FromString(etag.Value.AsSpan()[1..^1]);

				if (requestedCheckpoint > checkpoint) {
					return Results.Extensions.PreconditionFailed().ExecuteAsync(context);
				}
			}

			context.Response.Headers.ETag = checkpoint.ToString();
		}

		var inner = requestHeaders.Accept.Count switch {
			0 => new HalJsonResult(resource, hal),
			_ => requestHeaders.Accept.Select(MediaType).Select(Negotiate).FirstOrDefault() ??
			     Results.Extensions.NotAcceptable()
		};

		return inner.ExecuteAsync(context);

		IResult Negotiate(MediaType m) =>
			(HalJson.IsSubsetOf(m), Html.IsSubsetOf(m) || m.SubTypeSuffix == "html") switch {
				(true, false) => new HalJsonResult(resource, hal),
				(false, true) => new HalHtmlResult(resource, hal),
				_ => NotAcceptableResult.Instance
			};

		static MediaType MediaType(MediaTypeHeaderValue value) => new(value.MediaType);
	}

	private class HalJsonResult(T? resource, IHal hal) : IResult {
		private static readonly JsonSerializerOptions SerializerOptions
			= new() {
				Converters = {
					new LinksConverter(),
					new HalRepresentationConverter()
				},

				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			};

		private static readonly MediaTypeHeaderValue ContentType = new($"{HalJson.Type}/{HalJson.SubType}");

		public async Task ExecuteAsync(HttpContext context) {
			context.Response.ContentType = HalJsonContentType;
			var representation = await hal.RepresentationOfAsync(resource ?? new object());
			await context.Response.WriteAsJsonAsync(representation, SerializerOptions, context.RequestAborted);
		}
	}

	private class HalHtmlResult(T? resource, IHal hal) : IResult {
		private readonly T? _resource = resource;
		private readonly IHal _hal = hal;

		public HttpStatusCode StatusCode { get; init; }

		public Task ExecuteAsync(HttpContext context) {
			context.Response.StatusCode = (int)StatusCode;
			context.Response.ContentType = HalHtmlContentType;

			return Task.CompletedTask;
		}
	}
}
