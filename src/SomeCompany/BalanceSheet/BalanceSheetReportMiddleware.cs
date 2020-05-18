using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Hallo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SomeCompany.BalanceSheet;
using Transacto;
using MidFunc = System.Func<Microsoft.AspNetCore.Http.HttpContext, System.Func<System.Threading.Tasks.Task>,
	System.Threading.Tasks.Task>;

// ReSharper disable CheckNamespace
namespace Microsoft.AspNetCore.Builder {
// ReSharper restore CheckNamespace\
	internal static class BalanceSheetReportMiddleware {
		public static IEndpointRouteBuilder UseBalanceSheet(this IEndpointRouteBuilder builder) =>
			builder.MapGet("{thru}",
				async (DateTime thru, CancellationToken ct) => {
					var connectionFactory = builder.ServiceProvider
						.GetRequiredService<Func<CancellationToken, ValueTask<NpgsqlConnection>>>();
					await using var connection = await connectionFactory(ct);

					return new HalResponse(BalanceSheetRepresentation.Instance, await connection.QueryAsync<Item>(
						$@"
SELECT {Schema.BalanceSheetReport.Columns.AccountNumber},
       SUM({Schema.BalanceSheetReport.Columns.Balance}) as {Schema.BalanceSheetReport.Columns.Balance} 
FROM {Schema.BalanceSheetReport.Table}
WHERE date ({Schema.BalanceSheetReport.Columns.PeriodYear} || '-' ||{Schema.BalanceSheetReport.Columns.PeriodMonth} || '-01') <= @thru
GROUP BY {Schema.BalanceSheetReport.Columns.AccountNumber}", new {thru}));
				});

		private class BalanceSheetRepresentation : Hal<IEnumerable<Item>>,
			IHalState<IEnumerable<Item>> {
			public static readonly BalanceSheetRepresentation Instance = new BalanceSheetRepresentation();
			public object StateFor(IEnumerable<Item> resource) => resource.ToArray();
		}

		public class Item {
			public int AccountNumber { get; set; }
			public decimal Balance { get; set; }
		}
	}
}
