using Projac;
using Transacto.Framework;
using Transacto.Framework.Projections;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;
// ReSharper restore CheckNamespace

public static partial class ServiceCollectionExtensions {
	public static IServiceCollection AddInMemoryProjection<T>(this IServiceCollection services,
		Func<T, object, T> handler) where T : MemoryReadModel, new() =>
		services.AddSingleton<ProjectionHandler<InMemoryProjectionDatabase>[]>(
			new AnonymousProjectionBuilder<InMemoryProjectionDatabase>().When<Envelope>((target, envelope) => {
					var model = target.Get<T>();
					var (message, position) = envelope;

					target.Set(handler(model.HasValue ? model.Value : new T(), message) with {
						Checkpoint = position
					});
				})
				.Build());
}
