using System.Data.SQLite;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class SQLiteConnectionStringBuilderFactories {
	public static SQLiteConnectionStringBuilder OnDisk(Type projectionType) => new() {
		JournalMode = SQLiteJournalModeEnum.Wal,
		SyncMode = SynchronizationModes.Off,
		DataSource = $"{projectionType.FullName}.db"
	};
}
