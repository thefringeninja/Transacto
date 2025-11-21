using System.Collections.Immutable;
using System.Data.SQLite;
using Dapper;
using NodaTime;

namespace Transacto.Plugins.FinancialStatements.BalanceSheet;

internal static class Services {
	public static async Task<BalanceSheetReports> GetBalanceSheetReportsAsync(Func<SQLiteConnection> connectionFactory,
		ImmutableArray<LocalDate> thru, CancellationToken ct) {
		const string balancesSql =
			"""
			SELECT b.account_number, SUM(b.balance)
			FROM balance_sheet_daily b
			WHERE b.date <= @thru
			GROUP BY b.account_number
			ORDER BY b.account_number
			""";

		const string accountsSql =
			"""
			SELECT account_number, account_name
			FROM account_names
			""";
		await using var connection = connectionFactory();

		var accounts = await connection
			.QueryUnbufferedAsync<(int AccountNumber, string accountName)>(accountsSql)
			.ToDictionaryAsync(a => a.AccountNumber, a => a.accountName, ct);

		var reports = await thru.ToAsyncEnumerable()
			.SelectAwaitWithCancellation(GetReportAsync)
			.ToArrayAsync(cancellationToken: ct);

		return new BalanceSheetReports(accounts.ToImmutableDictionary(), [..reports]);

		async ValueTask<BalanceSheetReport> GetReportAsync(LocalDate thru, CancellationToken ct) {
			var result = connection.QueryUnbufferedAsync<(int accountNumber, decimal balance)>(balancesSql, new {
				thru = thru.ToDateTimeUnspecified()
			});

			return await result.AggregateAsync(BalanceSheetReport.For(thru),
				(report, item) => report with {
					AccountBalances = report.AccountBalances.Add(new(item.accountNumber, item.balance / 100m))
				}, cancellationToken: ct);
		}
	}
}
