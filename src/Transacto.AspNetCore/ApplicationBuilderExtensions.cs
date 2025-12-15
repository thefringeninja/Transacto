using Microsoft.AspNetCore.Mvc;
using Serilog.Context;
using Transacto;
using Transacto.Framework;
using Transacto.Framework.Http;
using Transacto.Framework.Projections;
using Transacto.Plugins;
using Index = Transacto.Plugins.Index;

// ReSharper disable CheckNamespace
namespace Microsoft.AspNetCore.Builder;
// ReSharper restore CheckNamespace

static partial class ApplicationBuilderExtensions {
	extension(IApplicationBuilder app) {
		public IApplicationBuilder UseTransacto(params IPlugin[] additionalPlugins) {
			var plugins = additionalPlugins.Concat(Standard.Plugins).ToArray();

			return plugins.Aggregate(app, (inner, plugin) => inner.Map(
					$"/{plugin.Slug}", app => app
						.Use(async (context, next) => {
							using (LogContext.PushProperty("Plugin", plugin.Name)) {
								await next(context);
							}
						})
						.Use((context, next) => {
							context.RequestServices =
								app.ApplicationServices.GetRequiredKeyedService<IServiceProvider>(plugin);
							return next();
						})
						.UseRouting()
						.UseOutputCache()
						.UseEndpoints(plugin.Configure)
						.UseProjectionStatus()))
				.Map("", inner => inner.UseTransactoIndex(plugins));
		}

		private void UseTransactoIndex(params IPlugin[] plugins) => app
			.UseRouting()
			.UseEndpoints(builder => {
				var resource = Index.Create(plugins);

				builder.MapGet("",
					([FromServices] IndexRepresentation representation) =>
						Results.Hal(resource, Checkpoint.None, representation));
			});
	}

	extension(IEndpointConventionBuilder app) {
		public IEndpointConventionBuilder CacheControl(RequestDelegate cachePolicy) =>
			app.AddEndpointFilter(async (context, next) => {
				context.HttpContext.Response.OnStarting(async static state => {
					var (context, cachePolicy) = ((HttpContext, RequestDelegate))state;

					await cachePolicy(context);
				}, (context.HttpContext, cachePolicy));

				return await next(context);
			});
	}
}
