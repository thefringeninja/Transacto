using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
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

        public async Task<SomeCompany.Framework.Http.Response> Get(DateTime thru, CancellationToken cancellationToken = default) {
            using var connection = _connectionFactory();

            var results = await connection.QueryAsync<Item>($@"
SELECT {Schema.BalanceSheetReport.Columns.AccountNumber},
       SUM({Schema.BalanceSheetReport.Columns.Balance}) as {Schema.BalanceSheetReport.Columns.Balance} 
FROM {_schema}.{Schema.BalanceSheetReport.Table}
WHERE date ({Schema.BalanceSheetReport.Columns.PeriodYear} || '-' ||{Schema.BalanceSheetReport.Columns.PeriodMonth} || '-01') <= @thru
GROUP BY {Schema.BalanceSheetReport.Columns.AccountNumber}", new {thru});

            return new Response(results);
        }

        private class Item {
            public int AccountNumber { get; set; }
            public decimal Balance { get; set; }
        }

        private class Response : SomeCompany.Framework.Http.Response {
            private readonly IEnumerable<Item> _items;

            public Response(IEnumerable<Item> items) : base(HttpStatusCode.OK,
                new MediaTypeHeaderValue("application/json")) {
                _items = items;
            }

            public override Task WriteBody(Stream stream, CancellationToken cancellationToken = default) =>
                JsonSerializer.SerializeAsync(stream, _items, new JsonSerializerOptions(), cancellationToken);
        }
    }
}
