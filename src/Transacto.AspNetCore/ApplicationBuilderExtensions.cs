using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Inflector;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Transacto;
using Transacto.Framework;
using Transacto.Framework.Http;
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
			builder.MapGet(route, async context => {
				var response = await getResponse(context);
				if (response is IHaveEventStorePosition hasPosition) {
					var positions = context.Request
						.GetTypedHeaders().IfMatch
						.Where(etag => etag.Tag.HasValue)
						.Select(etag => Checkpoint.FromString(etag!.Tag.Value.AsSpan()[1..^1]).ToEventStorePosition())
						.OrderByDescending(p => p)
						.ToList();

					if (positions.Count == 0) {
						await response.Write(context.Response);
						return;
					}

					if (hasPosition.Position.HasValue && positions.Any(requestedPosition => hasPosition.Position.Value >= requestedPosition)) {
						await response.Write(context.Response);
						return;
					}

					await PreconditionFailedResponse.Instance.Write(context.Response);

					return;
				}

				await response.Write(context.Response);
			});

			return builder;
		}

		public static IEndpointRouteBuilder MapPost<T>(this IEndpointRouteBuilder builder, string route,
			Func<HttpContext, T, ValueTask<Response>> getResponse) {
			builder.MapPost(route, async context => {
				var request = await JsonSerializer.DeserializeAsync<T>(context.Request.Body);

				var response = await getResponse(context, request!);

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
				var request = await JsonSerializer.DeserializeAsync<T>(context.Request.Body);

				var response = await getResponse(context, request!);

				await response.Write(context.Response);
			});
			return builder;
		}
	}
}
