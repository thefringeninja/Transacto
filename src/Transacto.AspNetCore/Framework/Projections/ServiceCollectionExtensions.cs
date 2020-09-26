using Projac;
using Transacto.Framework.Projections;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection {
	// ReSharper restore CheckNamespace

	public static partial class ServiceCollectionExtensions {
		public static IServiceCollection AddInMemoryProjection<T>(this IServiceCollection services,
			ProjectionHandler<InMemorySession>[] projection) where T : class, IMemoryReadModel, new() {
			var readModel = new T();
			return services
				.AddSingleton(projection)
				.AddSingleton<IMemoryReadModel>(readModel)
				.AddSingleton(readModel);
		}
	}
}
