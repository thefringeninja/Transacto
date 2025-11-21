using System.Data.SQLite;
using EventStore.Client;
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
		var connectionStringBuilder = new SQLiteConnectionStringBuilder {
			DataSource = $"{projection.GetType().FullName}.db",
			JournalMode = SQLiteJournalModeEnum.Wal,
			SyncMode = SynchronizationModes.Off
		};
		return services
			.AddKeyedSingleton<Func<SQLiteConnection>>(projection.GetType(),
				() => new SQLiteConnection(connectionStringBuilder.ConnectionString))
			.AddSingleton<IHostedService, SQLiteProjectionHost>(provider => new SQLiteProjectionHost(
				provider.GetRequiredService<EventStoreClient>(),
				provider.GetRequiredService<IMessageTypeMapper>(),
				provider.GetRequiredKeyedService<Func<SQLiteConnection>>(projection.GetType()), projection,
				provider.GetRequiredService<ILoggerFactory>().CreateLogger<SQLiteProjectionHost>()));
	}
}
