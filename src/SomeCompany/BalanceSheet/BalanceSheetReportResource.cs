using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using SomeCompany.Framework.Http;

namespace SomeCompany.BalanceSheet {
    public class BalanceSheetReportResource {
        private readonly Func<IDbConnection> _connectionFactory;
        private readonly string _schema;

        public BalanceSheetReportResource(Func<IDbConnection> connectionFactory, string schema) {
            if (connectionFactory == null) throw new ArgumentNullException(nameof(connectionFactory));
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            _connectionFactory = connectionFactory;
            _schema = schema;
        }

        public async Task<Response> Get(DateTime thru, CancellationToken cancellationToken = default) {
            using var connection = _connectionFactory();

            var results = await connection.QueryAsync<Item>($@"
SELECT {Schema.BalanceSheetReport.Columns.AccountNumber},
       SUM({Schema.BalanceSheetReport.Columns.Balance}) as {Schema.BalanceSheetReport.Columns.Balance} 
FROM {_schema}.{Schema.BalanceSheetReport.Table}
WHERE date ({Schema.BalanceSheetReport.Columns.PeriodYear} || '-' ||{Schema.BalanceSheetReport.Columns.PeriodMonth} || '-01') <= @thru
GROUP BY {Schema.BalanceSheetReport.Columns.AccountNumber}", new {thru});

            return new JsonResponse(results);
        }

        private class Item {
            public int AccountNumber { get; set; }
            public decimal Balance { get; set; }
        }

    }
}
