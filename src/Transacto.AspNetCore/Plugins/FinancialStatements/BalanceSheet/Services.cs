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
			.QueryUnbufferedAsync<(int accountNumber, string accountName)>(
				"""
				SELECT account_number, account_name
				FROM account_names
				""")
			.ToDictionaryAsync(a => a.accountNumber, a => new AccountInfo(a.accountNumber, a.accountName),
				cancellationToken: ct);

		var reports = await thru.ToAsyncEnumerable()
			.Select(GetReportAsync)
			.ToArrayAsync(cancellationToken: ct);

		return new BalanceSheetReports([..reports]);

		async ValueTask<ReportGroup> GetReportAsync(LocalDate thru, CancellationToken ct) {
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

			var lines = await result.AggregateAsync(new List<Report.Line>(), (acc, line) => {
				acc.Add(new Report.Line(accounts[line.accountNumber], line.balance / 100m));
				return acc;
			}, cancellationToken: ct);

			return ReportGroup.Create(thru, [..lines]);
		}
	}
}
