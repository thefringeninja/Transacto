using Projac;
using Transacto.Framework;
using Transacto.Framework.Projections;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;
// ReSharper restore CheckNamespace

public static partial class ServiceCollectionExtensions {
	extension(IServiceCollection services) {
		public IServiceCollection AddInMemoryProjection<T>(Func<T, object, T> handler)
			where T : MemoryReadModel, new() =>
			services.AddSingleton<InMemoryProjection>(new InMemoryProjection<T>(handler));
		public IServiceCollection AddProjectionHost<T>() where T : class, IHostedService, IProjectionHostStatus =>
			services
				.AddSingleton<T>()
				.AddHostedService<T>(provider => provider.GetRequiredService<T>())
				.AddSingleton<IProjectionHostStatus, ProjectionHostStatusWrapper<T>>();

		public IServiceCollection AddProjectionHost<T>(Func<IServiceProvider, T> factory)
			where T : class, IHostedService, IProjectionHostStatus => services
			.AddSingleton(factory)
			.AddSingleton<IHostedService>(provider => provider.GetRequiredService<T>())
			.AddSingleton<IProjectionHostStatus, ProjectionHostStatusWrapper<T>>();
	}
}
