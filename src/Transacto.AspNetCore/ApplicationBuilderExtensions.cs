using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EventStore.Client;
using Inflector;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Transacto;
using Transacto.Plugins;

// ReSharper disable CheckNamespace
namespace Microsoft.AspNetCore.Builder {
	// ReSharper restore CheckNamespace

	static partial class ApplicationBuilderExtensions {
		public static IApplicationBuilder UseTransacto(this IApplicationBuilder builder, params IPlugin[] plugins) =>
			plugins.Concat(Standard.Plugins)
				.Aggregate(builder,
					(inner, plugin) => inner.Map("/" + plugin.Name.Underscore().Dasherize(),
						builder => builder.Use((context, next) => {
								context.RequestServices = builder.ApplicationServices
									.GetServices<Tuple<IPlugin, IServiceProvider>>()
									.Where(x => x.Item1.Name == plugin.Name)
									.Select(x => x.Item2)
									.Single();
								return next();
							})
							.UseRouting()
							.UseEndpoints(plugin.Configure)));

		public static IEndpointRouteBuilder MapGet(this IEndpointRouteBuilder builder, string route,
			Func<HttpContext, ValueTask<Response>> getResponse) {
			var separator = new[] {'/'};
			builder.MapGet(route, async context => {
				var response = await getResponse(context);
				if (TryParsePosition(response.Headers.ETag, out var responsePosition)) {
					var positions = context.Request.GetTypedHeaders()
						.IfMatch
						.Where(etag => etag.Tag.HasValue)
						.Select(etag => TryParsePosition(etag, out var position) ? position : Position.Start)
						.OrderByDescending(p => p)
						.ToList();

					if (positions.Count == 0) {
						await response.Write(context.Response);
						return;
					}

					if (positions.Any(requestedPosition => responsePosition >= requestedPosition)) {
						await response.Write(context.Response);
						return;
					}

					await PreconditionFailedResponse.Instance.Write(context.Response);

					return;
				}

				await response.Write(context.Response);
			});

			return builder;

			bool TryParsePosition(EntityTagHeaderValue etag, out Position position) {
				position = default;
				var value = etag?.Tag.ToString();
				if (value == "*") {
					position = Position.Start;
					return true;
				}

				var parts = value?.Split(separator, 2) ?? Array.Empty<string>();

				if (parts.Length != 2 ||
				    !ulong.TryParse(parts[0][1..], out var p) ||
				    !ulong.TryParse(parts[1][..^1], out var c)) {
					return false;
				}

				position = new Position(p, c);
				return true;
			}
		}

		public static IEndpointRouteBuilder MapPost<T>(this IEndpointRouteBuilder builder, string route,
			Func<HttpContext, T, ValueTask<Response>> getResponse) {
			builder.MapPost(route, async context => {
				var request = (await JsonSerializer.DeserializeAsync<T>(context.Request.Body))!;

				var response = await getResponse(context, request);

				await response.Write(context.Response);
			});
			return builder;
		}

		public static IEndpointRouteBuilder MapPost(this IEndpointRouteBuilder builder, string route,
			Func<HttpContext, ValueTask<Response>> getResponse) {
			builder.MapPost(route, async context => {
				var response = await getResponse(context);

				await response.Write(context.Response);
			});
			return builder;
		}

		public static IEndpointRouteBuilder MapPut<T>(this IEndpointRouteBuilder builder, string route,
			Func<HttpContext, T, ValueTask<Response>> getResponse) {
			builder.MapPut(route, async context => {
				var request = (await JsonSerializer.DeserializeAsync<T>(context.Request.Body))!;

				var response = await getResponse(context, request);

				await response.Write(context.Response);
			});
			return builder;
		}
	}
}
