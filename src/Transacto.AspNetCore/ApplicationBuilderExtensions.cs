using Inflector;
using Transacto;
using Transacto.Plugins;

// ReSharper disable CheckNamespace
namespace Microsoft.AspNetCore.Builder;
// ReSharper restore CheckNamespace

static partial class ApplicationBuilderExtensions {
	public static IApplicationBuilder UseTransacto(this IApplicationBuilder builder, params IPlugin[] plugins) =>
		plugins.Concat(Standard.Plugins).Aggregate(builder, (inner, plugin) => inner.Map(
			$"/{plugin.Name.Underscore().Dasherize()}", builder => builder.Use((context, next) => {
					context.RequestServices =
						builder.ApplicationServices.GetRequiredKeyedService<IServiceProvider>(plugin);
					return next();
				})
				.UseRouting()
				.UseEndpoints(plugin.Configure)));
}
