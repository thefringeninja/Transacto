using System;
using EventStore.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Serilog;
using SqlStreamStore;

namespace Transacto {
	public class TransactoHost {
		public static IHostBuilder Build(IServiceProvider serviceProvider, params IPlugin[] plugins) =>
			new HostBuilder()
				.ConfigureLogging(builder => builder.AddSerilog())
				.ConfigureWebHost(builder => builder
					.UseKestrel()
					.Configure(app => app.UseTransacto(plugins))
					.ConfigureServices(services => services
						.AddSingleton(serviceProvider.GetRequiredService<EventStoreClient>())
						.AddSingleton(serviceProvider.GetRequiredService<NpgsqlConnectionStringBuilder>())
						.AddSingleton(serviceProvider.GetRequiredService<IStreamStore>())
						.AddTransacto(plugins)));
	}
}
