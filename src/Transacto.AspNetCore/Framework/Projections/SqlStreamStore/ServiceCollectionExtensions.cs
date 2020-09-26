using Transacto.Framework.Projections.SqlStreamStore;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection {
	// ReSharper restore CheckNamespace

	public static partial class ServiceCollectionExtensions {
		public static IServiceCollection AddStreamStoreProjection<T>(this IServiceCollection services)
			where T : StreamStoreProjection => services.AddSingleton<StreamStoreProjection, T>();

		public static IServiceCollection AddStreamStoreProjection(this IServiceCollection services,
			StreamStoreProjection projection)
			=> services.AddSingleton(projection);
	}
}
