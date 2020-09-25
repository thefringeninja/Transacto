using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Projac;
using SqlStreamStore;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;
using Transacto.Modules;
using Transacto.Plugins;

namespace Transacto {
	public static class ServiceCollectionExtensions {
		public static IServiceCollection AddStreamStoreProjection<T>(this IServiceCollection services)
			where T : StreamStoreProjection => services.AddSingleton<StreamStoreProjection, T>();

		public static IServiceCollection AddStreamStoreProjection(this IServiceCollection services,
			StreamStoreProjection projection)
			=> services.AddSingleton(projection);

		public static IServiceCollection AddNpgSqlProjection<T>(this IServiceCollection services)
			where T : NpgsqlProjection, new() => services.AddNpgSqlProjection(new T());

		public static IServiceCollection AddNpgSqlProjection(this IServiceCollection services,
			NpgsqlProjection projection) => services.AddSingleton(projection);

		public static IServiceCollection AddInMemoryProjection<T>(this IServiceCollection services,
			ProjectionHandler<InMemorySession>[] projection) where T : class, IMemoryReadModel, new() {
			var readModel = new T();
			return services
				.AddSingleton(projection)
				.AddSingleton<IMemoryReadModel>(readModel)
				.AddSingleton(readModel);
		}

		public static IServiceCollection AddTransacto(this IServiceCollection services, params IPlugin[] plugins) =>
			plugins.Concat(Standard.Plugins).Aggregate(services
					.AddRouting()
					.AddSingleton(MessageTypeMapper.Create(Array.ConvertAll(plugins,
						plugin => new MessageTypeMapper(plugin.MessageTypes))))
					// projections
					.AddSingleton<Func<IPlugin, NpgsqlConnection>>(provider => {
						var cache = new ConcurrentDictionary<string, NpgsqlConnectionStringBuilder>();
						return plugin => new NpgsqlConnection(cache.GetOrAdd(plugin.Name, username =>
							new NpgsqlConnectionStringBuilder(
								provider.GetRequiredService<NpgsqlConnectionStringBuilder>().ConnectionString) {
								Username = username
							}).ConnectionString);
					}),
				(services, plugin) => {
					var rootProvider = services.BuildServiceProvider();
					var pluginServices = new ServiceCollection();
					plugin.ConfigureServices(pluginServices);

					var pluginProvider = pluginServices
						.AddSingleton(rootProvider.GetRequiredService<EventStoreClient>())
						.AddSingleton(rootProvider.GetRequiredService<IStreamStore>())
						.AddSingleton(rootProvider.GetRequiredService<IMessageTypeMapper>())
						.AddSingleton<InMemorySession>()
						.AddHostedService(provider => new InMemoryProjectionHost(
							provider.GetRequiredService<EventStoreClient>(),
							provider.GetRequiredService<IMessageTypeMapper>(),
							provider.GetRequiredService<InMemorySession>(),
							provider.GetServices<ProjectionHandler<InMemorySession>[]>().ToArray()))
						.AddSingleton<Func<NpgsqlConnection>>(provider => () => rootProvider
							.GetRequiredService<Func<IPlugin, NpgsqlConnection>>()
							.Invoke(plugin))
						.AddHostedService(provider => new NpgSqlProjectionHost(
							provider.GetRequiredService<EventStoreClient>(),
							provider.GetRequiredService<IMessageTypeMapper>(),
							provider.GetRequiredService<Func<NpgsqlConnection>>(),
							provider.GetServices<NpgsqlProjection>().ToArray()))
						.AddHostedService(provider => new StreamStoreProjectionHost(
							provider.GetRequiredService<EventStoreClient>(),
							provider.GetRequiredService<IMessageTypeMapper>(),
							provider.GetRequiredService<IStreamStore>(),
							provider.GetServices<StreamStoreProjection>().ToArray()))
						.BuildServiceProvider();
					return pluginProvider
						.GetServices<IHostedService>()
						.Aggregate(services.AddSingleton(Tuple.Create(plugin, (IServiceProvider)pluginProvider)),
							(services, service) => services.AddSingleton(service));
				});
	}
}
