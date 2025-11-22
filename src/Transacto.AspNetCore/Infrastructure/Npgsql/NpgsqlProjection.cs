using System.Data.Common;
using KurrentDB.Client;
using Npgsql;
using Projac.Npgsql;
using Projac.Sql;

namespace Transacto.Infrastructure.Npgsql;

public abstract class NpgsqlProjection(NpgsqlScripts scripts) : SqlProjection {
	protected NpgsqlScripts Scripts { get; } = scripts;
	protected static readonly NpgsqlSyntax Sql = new();

	protected void When<TEvent>() =>
		When<TEvent>(_ => Sql.NonQueryStatement(Scripts[typeof(TEvent)], []));

	protected void When<TEvent>(Func<TEvent, DbParameter[]> handler) =>
		When<TEvent>(e => Sql.NonQueryStatement(Scripts[typeof(TEvent)], handler(e)));

	protected void When<TEvent>(Func<TEvent, DbParameter[][]> handler) =>
		When<TEvent>(e =>
			Array.ConvertAll(handler(e), parameters => Sql.NonQueryStatement(Scripts[typeof(TEvent)], parameters)));

	public async Task<FromAll> ReadCheckpoint(NpgsqlConnection connection, CancellationToken cancellationToken) {
		await connection.OpenAsync(cancellationToken);
		var statement = Sql.QueryStatement(NpgsqlScripts.ReadCheckpoint, new { projection = GetType().Name });

		await using var command = new NpgsqlCommand(statement.Text, connection) {
			Parameters = { statement.Parameters }
		};
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		return !await reader.ReadAsync(cancellationToken)
			? FromAll.Start
			: FromAll.After(new Position(unchecked((ulong)reader.GetInt64(0)), unchecked((ulong)reader.GetInt64(1))));
	}

	public async Task WriteCheckpoint(NpgsqlTransaction transaction, Position checkpoint,
		CancellationToken cancellationToken) {
		var statement = unchecked(
			Sql.QueryStatement(NpgsqlScripts.WriteCheckpoint, new {
				projection = GetType().Name,
				commit = (long)checkpoint.CommitPosition,
				prepare = (long)checkpoint.PreparePosition
			}));

		await using var command = new NpgsqlCommand(statement.Text, transaction.Connection, transaction) {
			Parameters = { statement.Parameters }
		};

		await command.ExecuteNonQueryAsync(cancellationToken);
	}
}
