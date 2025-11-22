using System.Collections.Concurrent;
using System.Data.SQLite;
using KurrentDB.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Projac;
using SqlStreamStore;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;
using Transacto.Framework.Projections;
using Transacto.Infrastructure.EventStore;
using Transacto.Infrastructure.Npgsql;
using Transacto.Infrastructure.SQLite;
using Transacto.Infrastructure.SqlStreamStore;
using Transacto.Plugins;

namespace Transacto;

public static class ServiceCollectionExtensions {
	public static IServiceCollection AddTransacto(this IServiceCollection services, KurrentDBClient eventStore,
		NpgsqlConnectionStringBuilder connectionStringBuilder, IStreamStore streamStore, params IPlugin[] plugins) {
		services.TryAddSingleton<Func<Type, SQLiteConnectionStringBuilder>>(projectionType =>
			new SQLiteConnectionStringBuilder {
				JournalMode = SQLiteJournalModeEnum.Wal,
				SyncMode = SynchronizationModes.Off,
				DataSource = $"{projectionType.FullName}.db"
			});

		return plugins.Concat(Standard.Plugins).Aggregate(services
				.AddSingleton(eventStore)
				.AddSingleton(connectionStringBuilder)
				.AddSingleton(streamStore)
				.AddHostedService<EventLogging>()
				.AddRouting()
				.AddSingleton(MessageTypeMapper.Create(Array.ConvertAll(plugins,
					plugin => new MessageTypeMapper(plugin.MessageTypes))))
				// projections
				.AddSingleton<Func<IPlugin, NpgsqlConnection>>(provider => {
					var cache = new ConcurrentDictionary<string, NpgsqlConnectionStringBuilder>();
					return plugin => new NpgsqlConnection(cache.GetOrAdd(plugin.Name, static (username, provider) =>
						new NpgsqlConnectionStringBuilder(
							provider.GetRequiredService<NpgsqlConnectionStringBuilder>().ConnectionString) {
							Username = username
						}, provider).ConnectionString);
				}),
			(services, plugin) => {
				var rootProvider = services.BuildServiceProvider();
				var pluginServices = new ServiceCollection()
					.AddLogging(builder =>
						rootProvider.GetServices<ILoggerProvider>()
							.Aggregate(builder.ClearProviders(), (acc, provider) => acc.AddProvider(provider)));
				plugin.ConfigureServices(pluginServices);

				var pluginProvider = pluginServices
					.AddSingleton(provider => new CommandDispatcher(provider.GetServices<CommandHandlerModule>()))
					.AddSingleton(rootProvider.GetRequiredService<ILoggerFactory>())
					.AddSingleton(rootProvider.GetRequiredService<KurrentDBClient>())
					.AddSingleton(rootProvider.GetRequiredService<IStreamStore>())
					.AddSingleton(rootProvider.GetRequiredService<IMessageTypeMapper>())
					.AddSingleton<InMemoryProjectionDatabase>()
					.AddHostedService(provider => new InMemoryProjectionHost(
						provider.GetRequiredService<KurrentDBClient>(),
						provider.GetRequiredService<IMessageTypeMapper>(),
						provider.GetRequiredService<InMemoryProjectionDatabase>(),
						provider.GetRequiredService<ILoggerFactory>().CreateLogger<InMemoryProjectionHost>(),
						provider.GetServices<ProjectionHandler<InMemoryProjectionDatabase>[]>().ToArray()))
					.AddSingleton(provider => provider.GetRequiredService<Func<Type, SQLiteConnectionStringBuilder>>())
					.AddSingleton<Func<NpgsqlConnection>>(_ => () => rootProvider
						.GetRequiredService<Func<IPlugin, NpgsqlConnection>>()
						.Invoke(plugin))
					.AddHostedService(provider => new NpgsqlProjectionHost(
						provider.GetRequiredService<KurrentDBClient>(),
						provider.GetRequiredService<IMessageTypeMapper>(),
						provider.GetRequiredService<Func<NpgsqlConnection>>(),
						provider.GetRequiredService<ILoggerFactory>().CreateLogger<NpgsqlProjectionHost>(),
						provider.GetServices<NpgsqlProjection>().ToArray()))
					.AddHostedService(provider => new StreamStoreProjectionHost(
						provider.GetRequiredService<KurrentDBClient>(),
						provider.GetRequiredService<IMessageTypeMapper>(),
						provider.GetRequiredService<IStreamStore>(),
						provider.GetServices<StreamStoreProjection>().ToArray()))
					.BuildServiceProvider();
				return pluginProvider
					.GetServices<IHostedService>()
					.Aggregate(services.AddKeyedSingleton<IServiceProvider>(plugin, pluginProvider),
						(services, service) => services.AddSingleton(service));
			});
	}
}
