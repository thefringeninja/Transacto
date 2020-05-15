using System;
using Inflector;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Transacto;
using Transacto.Framework;

#nullable enable
namespace SomeCompany {
	public class Startup : IStartup {
		private readonly IPlugin[] _plugins;
		private readonly IMessageTypeMapper _messageTypeMapper;

		public Startup(IPlugin[] plugins) {
			_plugins = plugins;
			_messageTypeMapper = MessageTypeMapper.Create(
				Array.ConvertAll(_plugins, p => new MessageTypeMapper(p.MessageTypes)));
		}

		public void Configure(IApplicationBuilder app) {
			app.UseTransacto();
			foreach (var plugin in _plugins) {
				app.Map(plugin.Name.Dasherize(), builder => {
					var services = new ServiceCollection();
					plugin.ConfigureServices(services);
					builder.ApplicationServices = new ScopedServiceProvider(services, builder.ApplicationServices);
					builder.UseRouting().UseEndpoints(plugin.Configure);
				});
			}
		}

		public IServiceProvider ConfigureServices(IServiceCollection services) => services
			.AddTransacto(_messageTypeMapper)
			.BuildServiceProvider();

		private class ScopedServiceProvider : IServiceProvider {
			private readonly IServiceProvider _parent;
			private readonly IServiceProvider _inner;

			public ScopedServiceProvider(IServiceCollection services, IServiceProvider parent) {
				_parent = parent;
				_inner = services.BuildServiceProvider();
			}

			public object GetService(Type serviceType) => _inner.GetService(serviceType) ??
			                                              _parent.GetService(serviceType);
		}
	}
}
