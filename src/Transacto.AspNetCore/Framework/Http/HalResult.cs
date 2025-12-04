using System.Net;
using Hallo;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Transacto.Framework.Http.Html;
using Transacto.Views.Shared;

namespace Transacto.Framework.Http;

internal abstract class HalResult {
	protected const string HalJsonContentType = "application/hal+json";
	protected const string HalHtmlContentType = "text/html";
	protected static readonly MediaType HalJson = new(HalJsonContentType);
	protected static readonly MediaType Html = new(HalHtmlContentType);
}

internal class HalResult<T>(T? resource, Checkpoint checkpoint, IHal hal, HttpStatusCode statusCode = HttpStatusCode.OK)
	: HalResult, IResult {
	private static TransactoRazorEngine RazorLightEngine => new();

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

				if (requestedCheckpoint <= checkpoint) {
					continue;
				}
				context.Response.StatusCode = (int)HttpStatusCode.PreconditionFailed;
				return NegotiateFromHeaders(requestHeaders, default).ExecuteAsync(context);
			}

			context.Response.Headers.ETag = checkpoint.ToString();
		}

		var inner = NegotiateFromHeaders(requestHeaders, resource);

		return inner.ExecuteAsync(context);

		IResult NegotiateFromHeaders(RequestHeaders headers, T? resource) {
			return headers.Accept.Count switch {
				0 => new HalJsonResult(resource, hal),
				_ => headers.Accept.Select(MediaType).Select(NegotiateFromMediaType).FirstOrDefault() ??
				     NotAcceptableResult.Instance
			};
		}

		IResult NegotiateFromMediaType(MediaType m) =>
			(HalJson.IsSubsetOf(m), Html.IsSubsetOf(m) || m.SubTypeSuffix == "html") switch {
				(true, false) => new HalJsonResult(resource, hal),
				(false, true) => new HalHtmlResult(resource, hal),
				_ => NotAcceptableResult.Instance
			};

		static MediaType MediaType(MediaTypeHeaderValue value) => new(value.MediaType);

	}

	private class HalJsonResult(T? resource, IHal hal) : IResult {
		public async Task ExecuteAsync(HttpContext context) {
			var representation = await hal.RepresentationOfAsync(resource);
			await context.Response.WriteAsJsonAsync(representation,
				TransactoHttpSerializerOptions.HalJsonSerializerOptions, HalJsonContentType, context.RequestAborted);
		}
	}

	private class HalHtmlResult(T? resource, IHal hal) : IResult {
		public async Task ExecuteAsync(HttpContext context) {
			context.Response.ContentType = HalHtmlContentType;

			var representation = await hal.RepresentationOfAsync(resource);

			var viewBag = new {
				_links = representation.Links.ToArray(),
				_embedded = representation.Embedded
			};
			var compiledView = RazorLightEngine.Compile<T>();
			var compiledLayout = RazorLightEngine.Compile<Layout, object>();
			var compiledTemplate = new TransactoRazorEngineCompiledTemplate<T>(compiledView,
				new() {
					["Layout.cshtml"] = compiledLayout
				});
			var html = compiledTemplate.Run((T)representation.State, viewBag);
			await context.Response.WriteAsync(html, context.RequestAborted);
		}
	}
}
