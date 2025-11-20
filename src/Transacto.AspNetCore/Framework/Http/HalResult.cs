using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hallo;
using Hallo.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using NodaTime.Text;
using Transacto.Framework.Http.Html;
using Transacto.Views.Shared;

namespace Transacto.Framework.Http;

internal abstract class HalResult {
	protected const string HalJsonContentType = "application/hal+json";
	protected const string HalHtmlContentType = "text/html";
	protected static readonly MediaType HalJson = new(HalJsonContentType);
	protected static readonly MediaType Html = new(HalHtmlContentType);

	protected static readonly JsonSerializerOptions HalJsonSerializerOptions = new() {
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		IncludeFields = true,
		Converters = {
			new LinksConverter(),
			new HalRepresentationConverter(),
			new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
			new NodaPatternConverter<LocalDate>(LocalDatePattern.Iso),
			new NodaPatternConverter<Period>(PeriodPattern.NormalizingIso)
		},
	};
}

internal class HalResult<T>(T? resource, Checkpoint checkpoint, IHal hal, HttpStatusCode statusCode = HttpStatusCode.OK)
	: HalResult, IResult where T : class {
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
		public async Task ExecuteAsync(HttpContext context) {
			context.Response.ContentType = HalJsonContentType;
			var representation = await hal.RepresentationOfAsync(resource);
			await context.Response.WriteAsJsonAsync(representation, HalJsonSerializerOptions, context.RequestAborted);
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
