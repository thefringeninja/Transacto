using System.Collections.Immutable;
using System.Data.SQLite;
using Dapper;
using NodaTime;
using Transacto.Messages;
using Transacto.Plugins.FinancialStatements.ProfitAndLoss;

namespace Transacto.Plugins.FinancialStatements.CashFlow;

internal static class Services {
	public static async Task<CashFlowReports> CashFlowReportsAsync(Func<SQLiteConnection> connectionFactory,
		GetProfitAndLossReportsAsync getProfitAndLossReportsAsync,
		ImmutableArray<(LocalDate startingon, Period period)> reportRanges,
		CancellationToken ct) {
		await using var connection = connectionFactory();

		var accounts = await connection.QueryUnbufferedAsync<(
				int accountNumber,
				string accountName,
				CashFlowCategoryType cashFlowCategory)>(
				"""
				SELECT account_number, account_name, cash_flow_category
				FROM accounts
				WHERE affects_cash = false
				""")
			.ToDictionaryAsync(account => account.accountNumber, cancellationToken: ct);

		var netIncomes = await getProfitAndLossReportsAsync(reportRanges, ct);

		var reports = await reportRanges.ToAsyncEnumerable()
			.OrderBy(q => q.startingon)
			.Select(GetReportAsync)
			.ToArrayAsync(cancellationToken: ct);

		return new CashFlowReports([..reports]);

		async ValueTask<ReportGroup> GetReportAsync((LocalDate, Period) q, int index, CancellationToken ct) {
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
					CashFlowCategoryType cashFlowCategory)>(
					"""
					SELECT c.account_number, SUM(c.balance)
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
				.Select(a => new Report.Line(
					new AccountInfo(a.accountNumber, accounts[a.accountNumber].accountName),
					a.balance / 100m, accounts[a.accountNumber].cashFlowCategory))
				.ToArrayAsync(ct);

			return ReportGroup.Create(new ReportRange(start, period), startingCash, netIncomes.Groups[index].NetProfit,
				[..cashFlow]);
		}
	}
}
