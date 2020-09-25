using Microsoft.Extensions.DependencyInjection;
using Projac;

namespace Transacto.Framework.Projections {
	public static class InMemoryServiceCollectionExtensions {
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
