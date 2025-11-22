using System.Collections.Immutable;
using System.Data.SQLite;
using Dapper;
using NodaTime;
using Transacto.Messages;

namespace Transacto.Plugins.FinancialStatements.CashFlow;

internal static class Services {
	public static async Task<CashFlowReports> CashFlowReportsAsync(
		Func<SQLiteConnection> connectionFactory,
		IEnumerable<(LocalDate startingon, Period period)> reportRanges,
		CancellationToken ct) {
		await using var connection = connectionFactory();

		var accounts = await connection.QueryUnbufferedAsync<(
				int accountNumber,
				string accountName,
				DefineAccountCashFlowCategory cashFlowCategory)>(
				"""
				SELECT account_number, account_name, cash_flow_category
				FROM accounts
				WHERE affects_cash = false
				""")
			.ToArrayAsync(ct);

		var reports = await reportRanges.ToAsyncEnumerable()
			.Select(GetReportAsync)
			.ToArrayAsync(cancellationToken: ct);

		return new CashFlowReports(
			accounts.ToImmutableDictionary(
				account => account.accountNumber,
				account => new CashFlowReports.AccountInfo(account.accountNumber, account.accountName,
					account.cashFlowCategory)),
			[..reports]);

		async ValueTask<CashFlowReport> GetReportAsync((LocalDate, Period) q, CancellationToken ct) {
			var (start, period) = q;

			var startingCash = await connection.ExecuteScalarAsync<decimal>(
				"""
				SELECT SUM(c.balance)
				FROM cash_flow_daily c
					LEFT JOIN accounts a ON c.account_number = a.account_number 
				WHERE c.date < @start
					AND a.affects_cash = true
				""", new {
					start = start.ToDateTimeUnspecified(),
				}) / 100m;

			var cashFlow = await connection.QueryUnbufferedAsync<(
					int accountNumber,
					decimal balance,
					DefineAccountCashFlowCategory cashFlowCategory)>(
					"""
					SELECT c.account_number, SUM(c.balance), a.cash_flow_category
					FROM cash_flow_daily c
						LEFT JOIN accounts a ON c.account_number = a.account_number 
					WHERE c.date BETWEEN @start AND @end
						AND a.affects_cash = false
					GROUP BY c.account_number
					ORDER BY c.account_number
					""", new {
						start = start.ToDateTimeUnspecified(),
						end = (start + period - Period.FromDays(1)).ToDateTimeUnspecified()
					})
				.Select(a => new CashFlowReport.AccountBalance(a.accountNumber, a.balance / 100m, a.cashFlowCategory))
				.ToArrayAsync(ct);

			return new CashFlowReport(new ReportRange(start, period), startingCash, [..cashFlow]);
		}
	}
}
