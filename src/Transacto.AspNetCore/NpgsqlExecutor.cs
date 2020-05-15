using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Projac.Sql;
using Projac.Sql.Executors;

namespace SomeCompany.Infrastructure {
    public class NpgsqlExecutor : IAsyncSqlNonQueryCommandExecutor {
        private readonly Func<NpgsqlConnection> _connectionFactory;
        private readonly int _commandTimeout;

        public NpgsqlExecutor(Func<NpgsqlConnection> connectionFactory, int commandTimeout = 30) {
            _connectionFactory = connectionFactory;
            _commandTimeout = commandTimeout;
        }

        public Task ExecuteNonQueryAsync(SqlNonQueryCommand command) {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            return ExecuteNonQueryAsync(command, CancellationToken.None);
        }

        public async Task ExecuteNonQueryAsync(SqlNonQueryCommand command, CancellationToken cancellationToken) {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            await using var dbConnection = _connectionFactory();
            await dbConnection.OpenAsync(cancellationToken);
            try {
                await using var dbCommand = dbConnection.CreateCommand();
                dbCommand.Connection = dbConnection;
                dbCommand.CommandTimeout = _commandTimeout;
                dbCommand.CommandType = command.Type;
                dbCommand.CommandText = command.Text;
                dbCommand.Parameters.AddRange(command.Parameters);
                await dbCommand.ExecuteNonQueryAsync(cancellationToken);
            } finally {
                await dbConnection.CloseAsync();
            }
        }

        public Task<int> ExecuteNonQueryAsync(IEnumerable<SqlNonQueryCommand> commands) {
            if (commands == null)
                throw new ArgumentNullException(nameof(commands));
            return ExecuteNonQueryAsync(commands, CancellationToken.None);
        }

        public async Task<int> ExecuteNonQueryAsync(IEnumerable<SqlNonQueryCommand> commands,
            CancellationToken cancellationToken) {
            if (commands == null)
                throw new ArgumentNullException(nameof(commands));
            await using var dbConnection = _connectionFactory();
            await dbConnection.OpenAsync(cancellationToken);
            try {
                await using var dbCommand = dbConnection.CreateCommand();
                dbCommand.Connection = dbConnection;
                dbCommand.CommandTimeout = _commandTimeout;
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
            } catch {
                throw;
            } finally {
                dbConnection.Close();
            }
        }
    }
}
