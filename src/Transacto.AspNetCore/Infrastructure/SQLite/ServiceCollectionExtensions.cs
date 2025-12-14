using System.Data.SQLite;
using KurrentDB.Client;
using Transacto;
using Transacto.Framework;
using Transacto.Infrastructure.SQLite;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;
// ReSharper restore CheckNamespace

public static partial class ServiceCollectionExtensions {
	extension(IServiceCollection services) {
		public IServiceCollection AddSQLiteProjection<T>()
			where T : SQLiteProjection, new() {
			SQLiteProjection projection = new T();
			return services
				.AddKeyedSingleton<Func<SQLiteConnection>>(projection.GetType(),
					(provider, _) => {
						var connectionString =
							provider.GetRequiredService<Func<Type, SQLiteConnectionStringBuilder>>()(
									projection.GetType())
								.ConnectionString;

						return () => new SQLiteConnection(connectionString);
					})
				.AddProjectionHost(provider => new SQLiteProjectionHost<T>(provider.GetRequiredService<KurrentDBClient>(),
					provider.GetRequiredService<IMessageTypeMapper>(),
					provider.GetRequiredKeyedService<Func<SQLiteConnection>>(projection.GetType()),
					provider.GetRequiredService<IPluginDescriptor>(),
					projection,
					provider.GetRequiredService<ILoggerFactory>().CreateLogger<SQLiteProjectionHost<T>>()));
		}
	}
}
