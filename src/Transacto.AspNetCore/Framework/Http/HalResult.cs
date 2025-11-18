using System.Dynamic;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hallo;
using Hallo.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using RazorLight;

namespace Transacto.Framework.Http;

internal abstract class HalResult {
	protected const string HalJsonContentType = "application/hal+json";
	protected const string HalHtmlContentType = "text/html";
	protected static readonly MediaType HalJson = new(HalJsonContentType);
	protected static readonly MediaType Html = new(HalHtmlContentType);

	protected static RazorLightEngine RazorLightEngine = new RazorLightEngineBuilder()
		.UseEmbeddedResourcesProject(typeof(IPlugin).Assembly)
		.UseMemoryCachingProvider()
		.Build();

	protected static readonly JsonSerializerOptions HalJsonSerializerOptions = new() {
		Converters = {
			new LinksConverter(),
			new HalRepresentationConverter(),
			new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
		},
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};
}

internal class HalResult<T>(T? resource, Checkpoint checkpoint, IHal hal, HttpStatusCode statusCode = HttpStatusCode.OK)
	: HalResult, IResult {
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
		// ReSharper disable StaticMemberInGenericType
		private static readonly string Template = GetTemplate();
		// ReSharper restore StaticMemberInGenericType

		public async Task ExecuteAsync(HttpContext context) {
			context.Response.ContentType = HalHtmlContentType;

			var representation = await hal.RepresentationOfAsync(resource);
			
			var html = await RazorLightEngine.CompileRenderStringAsync(typeof(T).ToString(), Template,
				representation.State, new() {
					{"_links", representation.Links},
					{"_embedded", representation.Embedded}
				});

			await context.Response.WriteAsync(html, context.RequestAborted);
		}

		private static string GetTemplate() {
			using var stream = typeof(T).Assembly.GetManifestResourceStream($"{typeof(T).FullName}.cshtml");
			if (stream is null) throw new Exception($"Embedded resource {typeof(T).FullName} not found. BUG!");
			using var reader = new StreamReader(stream);
			return reader.ReadToEnd();
		}
	}
}

internal static class ExpandoObjectExtensions {
	extension(ExpandoObject e) {
		public void Add(string key, object? value) {
			IDictionary<string, object?> i = e;
			i.Add(key, value);
		}
	}
}
