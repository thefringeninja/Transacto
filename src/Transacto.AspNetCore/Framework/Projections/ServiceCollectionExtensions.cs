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
			new AnonymousProjectionBuilder<InMemoryProjectionDatabase>()
				.When<Envelope>((target, envelope) => {
					var optional = target.Get<T>();
					var (message, position) = envelope;

					var model = optional.HasValue ? optional.Value : new T();
					target.Set(handler(model, message) with {
						Checkpoint = position
					});
				})
				.Build());
}
