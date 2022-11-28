using System.Net;
using System.Text.Json;
using Hallo;
using Hallo.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace Transacto.Framework.Http;

internal class HalResult<T> : IResult {
	private const string HalJsonContentType = "application/hal+json";
	private const string HalHtmlContentType = "text/html";
	private static readonly MediaType HalJson = new(HalJsonContentType);
	private static readonly MediaType Html = new(HalHtmlContentType);

	private readonly T? _resource;
	private readonly Checkpoint _checkpoint;
	private readonly IHal _hal;
	private readonly HttpStatusCode _statusCode;

	public HalResult(T? resource, Checkpoint checkpoint, IHal hal, HttpStatusCode statusCode = HttpStatusCode.OK) {
		_resource = resource;
		_checkpoint = checkpoint;
		_hal = hal;
		_statusCode = statusCode;
	}

	public Task ExecuteAsync(HttpContext context) {
		context.Response.StatusCode = (int)_statusCode;

		var requestHeaders = context.Request.GetTypedHeaders();

		if (_checkpoint != default) {
			for (var i = 0; i < requestHeaders.IfMatch.Count; i++) {
				var etag = requestHeaders.IfMatch[i].Tag;
				if (!etag.HasValue) {
					continue;
				}

				var requestedCheckpoint = Checkpoint.FromString(etag.Value.AsSpan()[1..^1]);

				if (requestedCheckpoint > _checkpoint) {
					return Results.Extensions.PreconditionFailed().ExecuteAsync(context);
				}
			}

			context.Response.Headers.ETag = _checkpoint.ToString();
		}

		var inner = requestHeaders.Accept.Count switch {
			0 => new HalJsonResult(_resource, _hal),
			_ => requestHeaders.Accept.Select(MediaType).Select(Negotiate).FirstOrDefault() ??
			     Results.Extensions.NotAcceptable()
		};

		return inner.ExecuteAsync(context);

		IResult Negotiate(MediaType m) =>
			(HalJson.IsSubsetOf(m), Html.IsSubsetOf(m) || m.SubTypeSuffix == "html") switch {
				(true, false) => new HalJsonResult(_resource, _hal),
				(false, true) => new HalHtmlResult(_resource, _hal),
				_ => NotAcceptableResult.Instance
			};

		static MediaType MediaType(MediaTypeHeaderValue value) => new(value.MediaType);
	}

	private class HalJsonResult : IResult {
		private static readonly JsonSerializerOptions SerializerOptions
			= new() {
				Converters = {
					new LinksConverter(),
					new HalRepresentationConverter()
				},

				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			};

		private static readonly MediaTypeHeaderValue ContentType = new($"{HalJson.Type}/{HalJson.SubType}");

		private readonly T? _resource;
		private readonly IHal _hal;

		public HalJsonResult(T? resource, IHal hal) {
			_resource = resource;
			_hal = hal;
		}

		public async Task ExecuteAsync(HttpContext context) {
			context.Response.ContentType = HalJsonContentType;
			var representation = await _hal.RepresentationOfAsync(_resource ?? new object());
			await context.Response.WriteAsJsonAsync(representation, SerializerOptions, context.RequestAborted);
		}
	}

	private class HalHtmlResult : IResult {
		private readonly T? _resource;
		private readonly IHal _hal;

		public HttpStatusCode StatusCode { get; init; }

		public HalHtmlResult(T? resource, IHal hal) {
			_resource = resource;
			_hal = hal;
		}

		public Task ExecuteAsync(HttpContext context) {
			context.Response.StatusCode = (int)StatusCode;
			context.Response.ContentType = HalHtmlContentType;

			return Task.CompletedTask;
		}
	}
}
