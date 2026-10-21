using System.Text.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Transacto.Domain;
using Transacto.Framework.CommandHandling;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;
// ReSharper restore CheckNamespace

public static partial class ServiceCollectionExtensions {
	extension(IServiceCollection services) {
		public IServiceCollection AddCommandHandlerModule<T>()
			where T : CommandHandlerModule => services.AddSingleton<CommandHandlerModule, T>();

		public IServiceCollection AddBusinessTransaction<T>() where T : IBusinessTransaction {
			services.TryAddSingleton(MemoryGeneralLedgerEntrySequenceNumberGenerator.Factory);
			services.AddKeyedSingleton<IGeneralLedgerEntrySequenceNumberGenerator>(typeof(T),
				static (provider, _) => {
					var factory = provider.GetRequiredService<Func<Type, IGeneralLedgerEntrySequenceNumberGenerator>>();
					
					return factory(typeof(T));
				});
			return services;
		}
	}
}
