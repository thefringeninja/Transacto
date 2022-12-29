using Transacto.Infrastructure.Npgsql;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;
// ReSharper restore CheckNamespace

public static partial class ServiceCollectionExtensions {
	public static IServiceCollection AddNpgSqlProjection<T>(this IServiceCollection services)
		where T : NpgsqlProjection, new() => services.AddNpgSqlProjection(new T());

	public static IServiceCollection AddNpgSqlProjection(this IServiceCollection services,
		NpgsqlProjection projection) => services.AddSingleton(projection);
}
