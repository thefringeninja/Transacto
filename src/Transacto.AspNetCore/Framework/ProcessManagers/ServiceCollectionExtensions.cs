using KurrentDB.Client;
using Transacto;
using Transacto.Framework;
using Transacto.Framework.ProcessManagers;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;
// ReSharper restore CheckNamespace

public static partial class ServiceCollectionExtensions {
	public static IServiceCollection AddProcessManager<TProcessManager>(this IServiceCollection services,
		string checkpointStreamName) where TProcessManager : ProcessManagerEventHandlerModule => services
		.AddSingleton<TProcessManager>()
		// AddHostedService would dedupe on ProcessManagerHost and silently drop every process manager after the first.
	.AddSingleton<IHostedService>(provider => new ProcessManagerHost(
			provider.GetRequiredService<KurrentDBClient>(),
			provider.GetRequiredService<IMessageTypeMapper>(),
			checkpointStreamName,
			provider.GetRequiredService<TProcessManager>(),
			provider.GetRequiredService<IPluginDescriptor>(),
			provider.GetRequiredService<ILoggerFactory>().CreateLogger<ProcessManagerHost>()));
}
