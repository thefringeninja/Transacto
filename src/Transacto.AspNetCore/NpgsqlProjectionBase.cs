using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Npgsql;
using Projac.Npgsql;
using Projac.Sql;
using SomeCompany.Infrastructure;

namespace SomeCompany.Framework.Projections {
	public abstract class NpgsqlProjectionBase : SqlProjection {
		protected NpgsqlScripts Scripts { get; }
		public string Schema { set => Scripts.Schema = value; }
		protected static readonly NpgsqlSyntax Sql = new NpgsqlSyntax();

		protected NpgsqlProjectionBase(NpgsqlScripts scripts) {
			Scripts = scripts;
		}

		protected void When<TEvent>() =>
			When<TEvent>(e => Sql.NonQueryStatement(Scripts[typeof(TEvent)], Array.Empty<DbParameter>()));

		protected void When<TEvent>(Func<TEvent, DbParameter[]> handler) =>
			When<TEvent>(e => Sql.NonQueryStatement(Scripts[typeof(TEvent)], handler(e)));

		protected void When<TEvent>(Func<TEvent, DbParameter[][]> handler) =>
			When<TEvent>(e =>
				Array.ConvertAll(handler(e), parameters => Sql.NonQueryStatement(Scripts[typeof(TEvent)], parameters)));

		public async Task<Position> ReadCheckpoint(NpgsqlConnection connection,
			CancellationToken cancellationToken) {
			await connection.OpenAsync(cancellationToken);
			var statement = Sql.QueryStatement(NpgsqlScripts.ReadCheckpoint, new {projection = GetType().Name});

			await using var command = new NpgsqlCommand(statement.Text, connection) {
				Parameters = {statement.Parameters}
			};
			await using var reader = await command.ExecuteReaderAsync(cancellationToken);
			if (!await reader.ReadAsync(cancellationToken)) {
				return Position.Start;
			}

			unchecked {
				return new Position((ulong)reader.GetInt64(0), (ulong)reader.GetInt64(1));
			}
		}

		public async Task WriteCheckpoint(NpgsqlTransaction transaction, Position checkpoint,
			CancellationToken cancellationToken) {
			SqlQueryCommand statement;
			unchecked {
				statement = Sql.QueryStatement(NpgsqlScripts.WriteCheckpoint, new {
					projection = GetType().Name,
					commit = (long)checkpoint.CommitPosition,
					prepare = (long)checkpoint.PreparePosition
				});
			}

			await using var command = new NpgsqlCommand(statement.Text, transaction.Connection, transaction) {
				Parameters = {statement.Parameters}
			};

			await command.ExecuteNonQueryAsync(cancellationToken);
		}

		public static implicit operator SqlProjectionHandler[](
			NpgsqlProjectionBase instance) {
			return instance.Handlers;
		}
	}
}
