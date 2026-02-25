using System.Data;
using System.Data.Common;
using KurrentDB.Client;
using Npgsql;
using Projac.Npgsql;
using Projac.Sql;
using Transacto.Framework.Projections;

namespace Transacto.Infrastructure.Npgsql;

public abstract class NpgsqlProjection : SqlProjection {
	protected NpgsqlProjection(NpgsqlScripts scripts) {
		Scripts = scripts;
		When<CreateSchema>();
		When<DropSchema>();
	}

	protected NpgsqlScripts Scripts { get; }
	protected static readonly NpgsqlSyntax Sql = new();

	protected void When<TEvent>() =>
		When<TEvent>(_ => Sql.NonQueryStatement(Scripts[typeof(TEvent)]));

	protected void When<TEvent>(Func<TEvent, DbParameter[]> handler) =>
		When<TEvent>(e => new SqlNonQueryCommand(Scripts[typeof(TEvent)], handler(e), CommandType.Text));

	protected void When<TEvent>(Func<TEvent, DbParameter[][]> handler) =>
		When<TEvent>(e =>
			Array.ConvertAll(handler(e),
				parameters => new SqlNonQueryCommand(Scripts[typeof(TEvent)], parameters, CommandType.Text)));

	public async Task<FromAll> ReadCheckpoint(NpgsqlConnection connection, CancellationToken cancellationToken) {
		await using var command = new NpgsqlCommand(NpgsqlScripts.ReadCheckpoint, connection) {
			Parameters = {
				Sql.VarChar(GetType().Name, new NpgsqlVarCharSize(256)).ToDbParameter("projection")
			}
		};
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		return !await reader.ReadAsync(cancellationToken)
			? FromAll.Start
			: FromAll.After(new Position(unchecked((ulong)reader.GetInt64(0)), unchecked((ulong)reader.GetInt64(1))));
	}

	public async Task WriteCheckpoint(NpgsqlTransaction transaction, Position checkpoint,
		CancellationToken cancellationToken) {
		await using var command = new NpgsqlCommand(NpgsqlScripts.WriteCheckpoint, transaction.Connection, transaction) {
			Parameters = {
				unchecked(new NpgsqlParameter<long>("@commit", (long)checkpoint.CommitPosition)),
				unchecked(new NpgsqlParameter<long>("@prepare", (long)checkpoint.CommitPosition)),
				new NpgsqlParameter<string>("@projection", GetType().Name)
			}
		};

		await command.ExecuteNonQueryAsync(cancellationToken);
	}
}
