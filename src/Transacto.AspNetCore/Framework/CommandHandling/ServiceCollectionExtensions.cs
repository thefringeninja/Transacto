using Transacto.Framework.CommandHandling;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection {
	// ReSharper restore CheckNamespace

	public static partial class ServiceCollectionExtensions {
		public static IServiceCollection AddCommandHandlerModule<T>(this IServiceCollection services)
			where T : CommandHandlerModule => services.AddSingleton<CommandHandlerModule, T>();
	}
}
