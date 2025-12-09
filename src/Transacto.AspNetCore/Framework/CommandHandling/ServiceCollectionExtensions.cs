using Transacto.Framework.CommandHandling;
using Transacto.Plugins.GeneralLedger;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;
// ReSharper restore CheckNamespace

public static partial class ServiceCollectionExtensions {
	extension(IServiceCollection services) {
		public IServiceCollection AddCommandHandlerModule<T>()
			where T : CommandHandlerModule => services.AddSingleton<CommandHandlerModule, T>();

		public IServiceCollection AddBusinessTransactions()
			=> services.AddCommandHandlerModule<GeneralLedgerEntryModule>();
	}
}
