using Npgsql;
using Projac.Sql;
using Projac.Sql.Executors;

namespace Transacto.Infrastructure.Npgsql;

public class NpgsqlExecutor(Func<NpgsqlConnection> connectionFactory, int commandTimeout = 30)
	: IAsyncSqlNonQueryCommandExecutor {
	public Task ExecuteNonQueryAsync(SqlNonQueryCommand command) {
		ArgumentNullException.ThrowIfNull(command);
		return ExecuteNonQueryAsync(command, CancellationToken.None);
	}

	public async Task ExecuteNonQueryAsync(SqlNonQueryCommand command, CancellationToken cancellationToken) {
		ArgumentNullException.ThrowIfNull(command);

		await using var dbConnection = connectionFactory();
		await dbConnection.OpenAsync(cancellationToken);
		try {
			await using var dbCommand = dbConnection.CreateCommand();
			dbCommand.Connection = dbConnection;
			dbCommand.CommandTimeout = commandTimeout;
			dbCommand.CommandType = command.Type;
			dbCommand.CommandText = command.Text;
			dbCommand.Parameters.AddRange(command.Parameters);
			await dbCommand.ExecuteNonQueryAsync(cancellationToken);
		} finally {
			await dbConnection.CloseAsync();
		}
	}

	public Task<int> ExecuteNonQueryAsync(IEnumerable<SqlNonQueryCommand> commands) {
		ArgumentNullException.ThrowIfNull(commands);
		return ExecuteNonQueryAsync(commands, CancellationToken.None);
	}

	public async Task<int> ExecuteNonQueryAsync(IEnumerable<SqlNonQueryCommand> commands,
		CancellationToken cancellationToken) {
		ArgumentNullException.ThrowIfNull(commands);
		await using var dbConnection = connectionFactory();
		await dbConnection.OpenAsync(cancellationToken);
		try {
			await using var dbCommand = dbConnection.CreateCommand();
			dbCommand.Connection = dbConnection;
			dbCommand.CommandTimeout = commandTimeout;
			var count = 0;
			foreach (var command in commands) {
				dbCommand.CommandType = command.Type;
				dbCommand.CommandText = command.Text;
				dbCommand.Parameters.Clear();
				dbCommand.Parameters.AddRange(command.Parameters);
				await dbCommand.ExecuteNonQueryAsync(cancellationToken);
				count++;
			}

			return count;
		} finally {
			await dbConnection.CloseAsync();
		}
	}
}
