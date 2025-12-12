// ReSharper disable CheckNamespace

using KurrentDB.Client;
using Transacto.Framework;
using Transacto.Framework.ProcessManagers;
using Transacto.Infrastructure.EventStore;

namespace Microsoft.Extensions.DependencyInjection;
// ReSharper restore CheckNamespace

public static partial class ServiceCollectionExtensions {
	extension(IServiceCollection services) {
		public IServiceCollection AddEventStoreProjection<T>(
			Func<KurrentDBClient, CancellationToken, ValueTask<FromAll>> readCheckpointAsync)
			where T : ProcessManagerEventHandlerModule {
			return services.AddSingleton<T>().AddHostedService(provider => new EventStoreProjectionHost<T>(
				provider.GetRequiredService<KurrentDBClient>(),
				provider.GetRequiredService<IMessageTypeMapper>(),
				provider.GetRequiredService<T>(),
				readCheckpointAsync,
				provider.GetRequiredService<ILoggerFactory>().CreateLogger<EventStoreProjectionHost<T>>()));
		}

		public IServiceCollection AddEventStoreProjection<T>(Func<IServiceProvider, T> factory,
			Func<IServiceProvider, Func<KurrentDBClient, CancellationToken, ValueTask<FromAll>>> readCheckpointAsyncFactory)
			where T : ProcessManagerEventHandlerModule {
			return services.AddSingleton(factory).AddHostedService(provider => {
				var readCheckpointAsync = readCheckpointAsyncFactory(provider);
				return new EventStoreProjectionHost<T>(
					provider.GetRequiredService<KurrentDBClient>(),
					provider.GetRequiredService<IMessageTypeMapper>(),
					provider.GetRequiredService<T>(),
					readCheckpointAsync,
					provider.GetRequiredService<ILoggerFactory>().CreateLogger<EventStoreProjectionHost<T>>());
			});
		}
	}
}
