using System.Data.SQLite;
using Projac.Sql;
using Projac.Sql.Executors;

namespace Transacto.Infrastructure.SQLite;

public class SQLiteExecutor(Func<SQLiteConnection> connectionFactory, int commandTimeout = 30)
	: ISqlNonQueryCommandExecutor {
	public void ExecuteNonQuery(SqlNonQueryCommand command) {
		ArgumentNullException.ThrowIfNull(command);

		using var dbConnection = connectionFactory();
		dbConnection.Open();
		try {
			using var dbCommand = dbConnection.CreateCommand();
			dbCommand.Connection = dbConnection;
			dbCommand.CommandTimeout = commandTimeout;
			dbCommand.CommandType = command.Type;
			dbCommand.CommandText = command.Text;
			dbCommand.Parameters.AddRange(command.Parameters);
			dbCommand.ExecuteNonQuery();
		} finally {
			dbConnection.CloseAsync();
		}
	}

	public int ExecuteNonQuery(IEnumerable<SqlNonQueryCommand> commands) {
		ArgumentNullException.ThrowIfNull(commands);
		using var dbConnection = connectionFactory();
		dbConnection.Open();
		using var dbCommand = dbConnection.CreateCommand();
		dbCommand.Connection = dbConnection;
		dbCommand.CommandTimeout = commandTimeout;
		var count = 0;
		foreach (var command in commands) {
			dbCommand.CommandType = command.Type;
			dbCommand.CommandText = command.Text;
			dbCommand.Parameters.Clear();
			dbCommand.Parameters.AddRange(command.Parameters);
			dbCommand.ExecuteNonQuery();
			count++;
		}

		return count;
	}
}
