using System;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Serilog;
using SqlStreamStore;

namespace Transacto {
	public class TransactoHost {
		public static IHostBuilder Build(IServiceProvider serviceProvider, params IPlugin[] plugins) => Host
			.CreateDefaultBuilder()
			.ConfigureLogging(builder => builder.AddSerilog())
			.UseServiceProviderFactory(new AutofacServiceProviderFactory())
			.ConfigureWebHost(builder => builder
				.UseKestrel()
				.ConfigureServices(services => services
					.AddEventStoreClient()
					.AddSingleton(serviceProvider.GetRequiredService<NpgsqlConnectionStringBuilder>())
					.AddSingleton(serviceProvider.GetRequiredService<IStreamStore>()))
				.UseStartup(new Startup(plugins)));
	}
}
