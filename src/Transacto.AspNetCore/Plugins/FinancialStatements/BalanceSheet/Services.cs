using System.Collections.Immutable;
using System.Data.SQLite;
using Dapper;
using NodaTime;

namespace Transacto.Plugins.FinancialStatements.BalanceSheet;

internal static class Services {
	public static async Task<BalanceSheetReports> GetBalanceSheetReportsAsync(Func<SQLiteConnection> connectionFactory,
		ImmutableArray<LocalDate> thru, CancellationToken ct) {
		await using var connection = connectionFactory();

		var accounts = await connection
			.QueryUnbufferedAsync<(int AccountNumber, string accountName)>(
				"""
				SELECT account_number, account_name
				FROM account_names
				""")
			.ToDictionaryAsync(a => a.AccountNumber, a => a.accountName, cancellationToken: ct);

		var reports = await thru.ToAsyncEnumerable()
			.Select(GetReportAsync)
			.ToArrayAsync(cancellationToken: ct);

		return new BalanceSheetReports(accounts.ToImmutableDictionary(), [..reports]);

		async ValueTask<BalanceSheetReport> GetReportAsync(LocalDate thru, CancellationToken ct) {
			var result = connection.QueryUnbufferedAsync<(int accountNumber, decimal balance)>(
				"""
				SELECT b.account_number, SUM(b.balance)
				FROM balance_sheet_daily b
				WHERE b.date <= @thru
				GROUP BY b.account_number
				ORDER BY b.account_number
				""", new {
					thru = thru.ToDateTimeUnspecified()
				});

			return await result.AggregateAsync(BalanceSheetReport.For(thru),
				(report, item) => report with {
					AccountBalances = report.AccountBalances.Add(new(item.accountNumber, item.balance / 100m))
				}, cancellationToken: ct);
		}
	}
}
