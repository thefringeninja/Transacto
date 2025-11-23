using System.Collections.Concurrent;
using System.Data.SQLite;
using KurrentDB.Client;
using Npgsql;
using SqlStreamStore;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;
using Transacto.Framework.Projections;
using Transacto.Infrastructure.EventStore;
using Transacto.Infrastructure.Npgsql;
using Transacto.Infrastructure.SqlStreamStore;
using Transacto.Plugins;

namespace Transacto;

public static class ServiceCollectionExtensions {
	public static IServiceCollection AddTransacto(this IServiceCollection services, 
		KurrentDBClient eventStore,
		NpgsqlConnectionStringBuilder npgsqlConnectionStringBuilder, 
		Func<Type, SQLiteConnectionStringBuilder> sqliteConnectionStringBuilderFactory,
		IStreamStore streamStore,
		Func<ILoggerProvider> getLoggerProvider, params IPlugin[] plugins) {
		var npgsqlConnectionStringBuilderCache = new ConcurrentDictionary<string, NpgsqlConnectionStringBuilder>();

		var messageTypeMapper = MessageTypeMapper.Create(Array.ConvertAll(plugins,
			plugin => new MessageTypeMapper(plugin.MessageTypes)));

		return plugins.Concat(Standard.Plugins).Aggregate(services
				.AddSingleton(eventStore)
				.AddSingleton(npgsqlConnectionStringBuilder)
				.AddSingleton(streamStore)
				.AddHostedService<EventLogging>()
				.AddRouting()
				.AddSingleton(messageTypeMapper)
				// projections
				.AddSingleton<Func<IPlugin, NpgsqlConnection>>(NpqsqlConnectonFactory),
			(services, plugin) => {
				var pluginServices = new ServiceCollection();

				plugin.ConfigureServices(pluginServices);

				var pluginProvider = pluginServices
					.AddLogging(builder => builder.ClearProviders().AddProvider(getLoggerProvider()))
					.AddSingleton<CommandDispatcher>()
					.AddSingleton(eventStore)
					.AddSingleton(streamStore)
					.AddSingleton(messageTypeMapper)
					.AddSingleton<InMemoryProjectionDatabase>()
					.AddHostedService<InMemoryProjectionHost>()
					.AddHostedService<NpgsqlProjectionHost>()
					.AddHostedService<StreamStoreProjectionHost>()
					.AddSingleton(sqliteConnectionStringBuilderFactory)
					.AddSingleton<Func<NpgsqlConnection>>(() => NpqsqlConnectonFactory(plugin))
					.BuildServiceProvider();

				return services
					.AddKeyedSingleton<IServiceProvider>(plugin, pluginProvider)
					.AddSingleton<IHostedService>(_ => new HostedServicesBackgroundService(plugin, pluginProvider));
			});

		NpgsqlConnection NpqsqlConnectonFactory(IPlugin plugin) =>
			new(npgsqlConnectionStringBuilderCache.GetOrAdd(plugin.Name, username =>
				new NpgsqlConnectionStringBuilder(
					npgsqlConnectionStringBuilder.ConnectionString) {
					Username = username
				}).ConnectionString);
	}

	private class HostedServicesBackgroundService(IPlugin plugin, IServiceProvider serviceProvider) : IHostedService {
		public string Name => plugin.Name;
		public Task StartAsync(CancellationToken cancellationToken) =>
			Task.WhenAll(serviceProvider.GetServices<IHostedService>()
				.Select(service => service.StartAsync(cancellationToken)));


		public Task StopAsync(CancellationToken cancellationToken) =>
			Task.WhenAll(serviceProvider.GetServices<IHostedService>()
				.Select(service => service.StopAsync(cancellationToken)));
	}
}
