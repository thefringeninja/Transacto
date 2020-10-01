using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KellermanSoftware.CompareNetObjects;
using Npgsql;
using Projac.Sql;
using Projac.Sql.Executors;

namespace SomeCompany {
    internal static class XUnitExtensions {
        static readonly Inflector.Inflector Inflector = new Inflector.Inflector(CultureInfo.GetCultureInfo("en-US"));

        public static async Task Assert(this NpgsqlProjectionScenario scenario,
            NpgsqlConnectionStringBuilder? connectionStringBuilder = null) {
            if (scenario == null) throw new ArgumentNullException(nameof(scenario));

            connectionStringBuilder ??= new NpgsqlConnectionStringBuilder {
                Host = "localhost",
                Username = "postgres"
            };

            await using var sqlConnection = new NpgsqlConnection(connectionStringBuilder.ConnectionString);
            await sqlConnection.OpenAsync();
            var projector = new AsyncSqlProjector(
                scenario.Resolver,
                new ConnectedSqlCommandExecutor(sqlConnection));

            var result = await scenario.Verify(async connection => {
                try {
                    foreach (var schema in scenario.Schemas) {
                        await using var createSchema = connection.CreateCommand();
                        createSchema.CommandText = $@"CREATE SCHEMA IF NOT EXISTS {schema}";
                        await createSchema.ExecuteNonQueryAsync();
                    }

                    foreach (var message in scenario.Messages)
                        try {
                            await projector.ProjectAsync(message);
                        } catch {
                            throw;
                        }

                    var compare = new CompareLogic(new ComparisonConfig {
                        IgnoreCollectionOrder = true,
                        IgnoreObjectTypes = true
                    });

                    var comparisonResults = new List<ComparisonResult>();

                    for (var i = 0; i < scenario.Tables.Length; i++) {
                        var table = scenario.Tables[i];
                        var rows = new List<object>();
                        await using var command = connection.CreateCommand();
                        command.CommandText =
                            $@"SELECT {string.Join(", ", Array.ConvertAll(table.ColumnNames, Inflector.Underscore))} FROM {table.Schema}.{table.TableName}";
                        command.CommandType = CommandType.Text;
                        await using var reader = await command.ExecuteReaderAsync();
                        while (await reader.ReadAsync()) {
                            rows.Add(reader.ToObject(table.Type));
                        }

                        comparisonResults.Add(compare.Compare(table.Rows, rows.ToArray()));
                    }

                    return comparisonResults.All(x => x.AreEqual)
                        ? VerificationResult.Pass()
                        : VerificationResult.Fail(comparisonResults
                            .Aggregate(new StringBuilder("Expected stuff but got the following differences: "),
                                (builder, comparisonResult) => builder.Append(comparisonResult.DifferencesString))
                            .ToString());
                } finally {
                    foreach (var schema in scenario.Schemas) {
                        await using var createSchema = connection.CreateCommand();
                        createSchema.CommandText = $@"DROP SCHEMA IF EXISTS {schema} CASCADE";
                        await createSchema.ExecuteNonQueryAsync();
                    }
                }
            }).Verification(sqlConnection, CancellationToken.None);

            Xunit.Assert.True(result.Passed, result.Message);
        }

        private static object ToObject(this IDataRecord reader, Type type) {
            var item = Activator.CreateInstance(type)!;

            foreach (var property in type.GetProperties().Where(pi => pi.CanWrite)) {
                var ordinal = reader.GetOrdinal(Inflector.Underscore(property.Name));

                var value = reader[ordinal];
                if (value != DBNull.Value) {
                    property.SetValue(item, ChangeType(value, property.PropertyType));
                }
            }

            return item;
        }

        private static object? ChangeType(object? value, Type type) =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                ? value == null
                    ? null
                    : Convert.ChangeType(value, Nullable.GetUnderlyingType(type)!)
                : Convert.ChangeType(value, type);
    }
}
