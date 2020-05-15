using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Hallo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Npgsql;
using SomeCompany.BalanceSheet;
using SomeCompany.Framework.Http;
using MidFunc = System.Func<Microsoft.AspNetCore.Http.HttpContext, System.Func<System.Threading.Tasks.Task>,
	System.Threading.Tasks.Task>;

// ReSharper disable CheckNamespace
namespace Microsoft.AspNetCore.Builder {
// ReSharper restore CheckNamespace

	public static class BalanceSheetReportMiddleware {
		public static IEndpointRouteBuilder UseBalanceSheet(this IEndpointRouteBuilder builder,
			Func<CancellationToken, Task<NpgsqlConnection>> connectionFactory, string schema,
			PathString? location = null) {
			location ??= new PathString("/balance-sheet");

			return builder.MapGet(location.Value.Add("{thru}"),
				async (DateTime thru, CancellationToken ct) => {
					await using var connection = await connectionFactory(ct);

					return new HalResponse(BalanceSheetRepresentation.Instance, await connection.QueryAsync<Item>(
						$@"
SELECT {Schema.BalanceSheetReport.Columns.AccountNumber},
       SUM({Schema.BalanceSheetReport.Columns.Balance}) as {Schema.BalanceSheetReport.Columns.Balance} 
FROM {schema}.{Schema.BalanceSheetReport.Table}
WHERE date ({Schema.BalanceSheetReport.Columns.PeriodYear} || '-' ||{Schema.BalanceSheetReport.Columns.PeriodMonth} || '-01') <= @thru
GROUP BY {Schema.BalanceSheetReport.Columns.AccountNumber}", new {thru}));
				});
		}

		private class BalanceSheetRepresentation : Hal<IEnumerable<Item>>,
			IHalState<IEnumerable<Item>> {
			public static readonly BalanceSheetRepresentation Instance = new BalanceSheetRepresentation();
			public object StateFor(IEnumerable<Item> resource) => resource.ToArray();
		}

		private class Item {
			public int AccountNumber { get; set; }
			public decimal Balance { get; set; }
		}
	}
}
