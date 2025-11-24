using System.Data.SQLite;
using KurrentDB.Client;
using Transacto.Framework;
using Transacto.Infrastructure.SQLite;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;
// ReSharper restore CheckNamespace

public static partial class ServiceCollectionExtensions {
	extension(IServiceCollection services) {
		public IServiceCollection AddSQLiteProjection<T>()
			where T : SQLiteProjection, new() => services.AddSQLiteProjection(new T(), typeof(T).Name);

		public IServiceCollection AddSQLiteProjection(SQLiteProjection projection, string name) =>
			services
				.AddKeyedSingleton<Func<SQLiteConnection>>(projection.GetType(),
					(provider, _) => {
						var connectionString =
							provider.GetRequiredService<Func<Type, SQLiteConnectionStringBuilder>>()(
									projection.GetType())
								.ConnectionString;

						return () => new SQLiteConnection(connectionString);
					})
				.AddSingleton<IHostedService>(provider => new SQLiteProjectionHost(provider.GetRequiredService<KurrentDBClient>(),
					provider.GetRequiredService<IMessageTypeMapper>(),
					provider.GetRequiredKeyedService<Func<SQLiteConnection>>(projection.GetType()), projection,
					provider.GetRequiredService<ILoggerFactory>().CreateLogger<SQLiteProjectionHost>()));
	}
}

public static class SQLiteConnectionStringBuilderFactories {
	public static SQLiteConnectionStringBuilder OnDisk(Type projectionType) => new() {
		JournalMode = SQLiteJournalModeEnum.Wal,
		SyncMode = SynchronizationModes.Off,
		DataSource = $"{projectionType.FullName}.db"
	};
}
