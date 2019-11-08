using System;
using System.Data.Common;
using Projac.Npgsql;
using Projac.Sql;
using SomeCompany.Infrastructure;

namespace SomeCompany.Framework {
    public abstract class NpgsqlProjectionBase : SqlProjection {
        protected NpgsqlScripts Scripts { get; }
        protected static readonly NpgsqlSyntax Sql = new NpgsqlSyntax();

        protected NpgsqlProjectionBase(NpgsqlScripts scripts) {
            if (scripts == null) throw new ArgumentNullException(nameof(scripts));
            Scripts = scripts;
        }

        protected void When<TEvent>() =>
            When<TEvent>(e => Sql.NonQueryStatement(Scripts[typeof(TEvent)], Array.Empty<DbParameter>()));

        protected void When<TEvent>(Func<TEvent, DbParameter[]> handler) =>
            When<TEvent>(e => Sql.NonQueryStatement(Scripts[typeof(TEvent)], handler(e)));

        protected void When<TEvent>(Func<TEvent, DbParameter[][]> handler) =>
            When<TEvent>(e =>
                Array.ConvertAll(handler(e), parameters => Sql.NonQueryStatement(Scripts[typeof(TEvent)], parameters)));
    }
}
