using EventStore.Client;
using Transacto.Framework;
using Transacto.Framework.ProcessManagers;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;
// ReSharper restore CheckNamespace

public static partial class ServiceCollectionExtensions {
	public static IServiceCollection AddProcessManager<TProcessManager>(this IServiceCollection services,
		string checkpointStreamName) where TProcessManager : ProcessManagerEventHandlerModule => services
		.AddSingleton<TProcessManager>()
		.AddHostedService(provider => new ProcessManagerHost(
			provider.GetRequiredService<EventStoreClient>(),
			provider.GetRequiredService<IMessageTypeMapper>(),
			checkpointStreamName,
			provider.GetRequiredService<TProcessManager>()));
}
