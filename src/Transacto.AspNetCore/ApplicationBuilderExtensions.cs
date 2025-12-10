using Microsoft.AspNetCore.Mvc;
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
	public static IApplicationBuilder UseTransacto(this IApplicationBuilder app, params IPlugin[] additionalPlugins) {
		var plugins = additionalPlugins.Concat(Standard.Plugins).ToArray();
		return plugins.Aggregate(app, (inner, plugin) => inner.Map(
				$"/{plugin.Slug}", app => app.Use((context, next) => {
						context.RequestServices =
							app.ApplicationServices.GetRequiredKeyedService<IServiceProvider>(plugin);
						return next();
					})
					.UseRouting()
					.UseEndpoints(plugin.Configure)
					.UseProjectionStatus()))
			.Map("", inner => inner.UseTransactoIndex(plugins));
	}

	private static void UseTransactoIndex(this IApplicationBuilder app, params IPlugin[] plugins) => app
		.UseRouting()
		.UseEndpoints(builder => {
			var resource = Index.Create(plugins);

			builder.MapGet("",
				([FromServices] IndexRepresentation representation) =>
					Results.Hal(resource, Checkpoint.None, representation));
		});
}
