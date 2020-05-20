using System;
using System.Linq;
using System.Threading.Tasks;
using Inflector;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Transacto.Plugins;

namespace Transacto {
	public static partial class BuilderExtensions {
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
							.Use(
(context, next) => {
	context.Response.OnStarting(() => {
		if (context.Response.StatusCode == 401) {
			context.Response.Headers.Add("www-authenticate", "..");
		}
		return Task.CompletedTask;
	});
	return next();
})
							.UseRouting()
							.UseEndpoints(plugin.Configure)));
	}
}
