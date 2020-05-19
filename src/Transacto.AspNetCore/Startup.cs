using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Transacto {
	public class Startup : IStartup {
		private readonly IPlugin[] _plugins;

		public Startup(IPlugin[] plugins) {
			_plugins = plugins;
		}

		public void Configure(IApplicationBuilder app) => app.UseTransacto(_plugins);

		public IServiceProvider ConfigureServices(IServiceCollection services) => services
			.AddTransacto(_plugins).BuildServiceProvider();
	}
}
