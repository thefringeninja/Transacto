using Inflector;
using Transacto;
using Transacto.Plugins;

// ReSharper disable CheckNamespace
namespace Microsoft.AspNetCore.Builder;
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
}
