using EventStore.Client;
using Transacto.Framework;
using Transacto.Framework.CommandHandling;
using Transacto.Framework.ProcessManagers;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection {
	// ReSharper restore CheckNamespace

	public static partial class ServiceCollectionExtensions {
		public static IServiceCollection AddProcessManager(this IServiceCollection services,
			string checkpointStreamName) => services
			.AddHostedService(provider => new ProcessManagerHost(
				provider.GetRequiredService<EventStoreClient>(),
				provider.GetRequiredService<IMessageTypeMapper>(),
				checkpointStreamName,
				provider.GetServices<CommandHandlerModule>()));
	}
}
