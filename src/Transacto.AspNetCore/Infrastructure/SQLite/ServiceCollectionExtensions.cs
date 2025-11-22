using System.Data.SQLite;
using KurrentDB.Client;
using Transacto.Framework;
using Transacto.Infrastructure.SQLite;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;
// ReSharper restore CheckNamespace

public static partial class ServiceCollectionExtensions {
	public static IServiceCollection AddSQLiteProjection<T>(this IServiceCollection services)
		where T : SQLiteProjection, new() => services.AddSQLiteProjection(new T());

	public static IServiceCollection AddSQLiteProjection(
		this IServiceCollection services, SQLiteProjection projection) {
		return services
			.AddKeyedSingleton<Func<SQLiteConnection>>(projection.GetType(),
				(provider, _) => {
					var connectionString =
						provider.GetRequiredService<Func<Type, SQLiteConnectionStringBuilder>>()(projection.GetType())
							.ConnectionString;

					return () => new SQLiteConnection(connectionString);
				})
			.AddSingleton<IHostedService, SQLiteProjectionHost>(provider => new SQLiteProjectionHost(
				provider.GetRequiredService<KurrentDBClient>(),
				provider.GetRequiredService<IMessageTypeMapper>(),
				provider.GetRequiredKeyedService<Func<SQLiteConnection>>(projection.GetType()), projection,
				provider.GetRequiredService<ILoggerFactory>().CreateLogger<SQLiteProjectionHost>()));
	}
}
