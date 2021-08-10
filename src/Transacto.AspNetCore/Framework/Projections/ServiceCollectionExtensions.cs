using Projac;
using Transacto.Framework.Projections;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection {
	// ReSharper restore CheckNamespace

	public static partial class ServiceCollectionExtensions {
		public static IServiceCollection AddInMemoryProjection(this IServiceCollection services,
			ProjectionHandler<InMemoryProjectionDatabase>[] projection) => services.AddSingleton(projection);
	}
}
