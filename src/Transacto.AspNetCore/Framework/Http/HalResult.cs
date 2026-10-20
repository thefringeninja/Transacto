using System.Net;
using System.Reflection;
using AngleSharp;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using Hallo;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Transacto.Framework.Http.Html;
using Transacto.Plugins.FinancialStatements;
using Link = Transacto.Views.Shared.Link;

namespace Transacto.Framework.Http;

internal abstract class HalResult {
	protected const string HalJsonContentType = "application/hal+json";
	protected const string HalHtmlContentType = "text/html; charset=utf-8";
	protected static readonly MediaType HalJson = new(HalJsonContentType);
	protected static readonly MediaType Html = new(HalHtmlContentType);
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

				if (requestedCheckpoint <= checkpoint) {
					continue;
				}

				context.Response.StatusCode = (int)HttpStatusCode.PreconditionFailed;
				return NegotiateFromHeaders(requestHeaders, default, hal).ExecuteAsync(context);
			}

			context.Response.Headers.ETag = checkpoint.ToString();
		}

		var inner = NegotiateFromHeaders(requestHeaders, resource, hal);

		return inner.ExecuteAsync(context);

		static IResult NegotiateFromHeaders(RequestHeaders headers, T? resource, IHal hal) {
			return headers.Accept.Count switch {
				0 => new HalJsonResult(resource, hal),
				_ => headers.Accept.Select(MediaType).Select(NegotiateFromMediaType).FirstOrDefault() ??
				     NotAcceptableResult.Instance
			};
			
			IResult NegotiateFromMediaType(MediaType m) =>
				(HalJson.IsSubsetOf(m), Html.IsSubsetOf(m) || m.SubTypeSuffix == "html") switch {
					(true, false) => new HalJsonResult(resource, hal),
					(false, true) => new HalHtmlResult(resource, hal),
					_ => NotAcceptableResult.Instance
				};

		}


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
		private static readonly HtmlParser s_HtmlParser = new();

		private static readonly PrettyMarkupFormatter s_PrettyMarkupFormatter = new() {
			Indentation = "\t",
			NewLine = "\n"
		};

		private static IReadOnlyList<Type> GetLinkRenderers() =>
			typeof(T).Namespace is null
				? []
				: typeof(T).Assembly.GetTypes().Where(type =>
						type.Namespace is not null &&
						type.IsAssignableTo(typeof(ILinkMatcher)) &&
						type.Name.EndsWith("_link") &&
						type.Namespace.Split('.').Any(typeof(T).Namespace!.StartsWith))
					.ToList()
					.AsReadOnly();

		private static readonly IReadOnlyList<Type> s_LinkRenderers = GetLinkRenderers();

		public async Task ExecuteAsync(HttpContext context) {
			await using var htmlRenderer = new HtmlRenderer(context.RequestServices,
				context.RequestServices.GetRequiredService<ILoggerFactory>());
			context.Response.ContentType = HalHtmlContentType;

			var representation = await hal.RepresentationOfAsync(resource);

			var viewContext = context.RequestServices.GetRequiredService<ViewContext>();
			viewContext._links = representation.Links.ToArray();
			viewContext._embedded = representation.Embedded;
			viewContext.GetTemplatedLink = link => {
				foreach (var r in s_LinkRenderers) {
					if ((bool)r.GetMethod("IsMatch", BindingFlags.Public | BindingFlags.Static)!
					    .Invoke(null, [link])!) {
						return r;
					};
				}
				return typeof(Link);
			};

			var parameters = new Dictionary<string, object?> {
				["Model"] = representation.State
			};

			var viewTypeName = $"{typeof(T).FullName}_view".Replace('+', '_');
			var viewType = Type.GetType(viewTypeName);

			if (viewType is null) {
				viewType = typeof(ProblemDetails_view);
				parameters["Model"] = new ProblemDetails {
					Detail = $"The view {viewTypeName}, {typeof(T).Assembly} was not found and no html could be rendered.",
					Status = 406
				};
			}

			var html = await htmlRenderer.Dispatcher.InvokeAsync(async () => {
				var component =
					await htmlRenderer.RenderComponentAsync(viewType, ParameterView.FromDictionary(parameters));
				return component.ToHtmlString();
			});

			if (context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()) {
				html = s_HtmlParser.ParseDocument(html).ToHtml(s_PrettyMarkupFormatter);
			}

			await context.Response.WriteAsync(html, context.RequestAborted);
		}
	}
}
