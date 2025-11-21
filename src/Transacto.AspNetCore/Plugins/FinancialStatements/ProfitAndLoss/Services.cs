using System.Collections.Immutable;
using System.Data.SQLite;
using Dapper;
using NodaTime;

namespace Transacto.Plugins.FinancialStatements.ProfitAndLoss;

internal static class Services {
	public static async Task<ProfitAndLossReports> ProfitAndLossReportsAsync(Func<SQLiteConnection> connectionFactory,
		IEnumerable<(LocalDate startingon, Period period)> reportRanges, CancellationToken ct) {
		await using var connection = connectionFactory();

		var accounts = await connection
			.QueryUnbufferedAsync<(int AccountNumber, string accountName)>(
				"""
				SELECT account_number, account_name
				FROM account_names
				""")
			.ToDictionaryAsync(a => a.AccountNumber, a => a.accountName, ct);

		var reports = await reportRanges.ToAsyncEnumerable()
			.SelectAwaitWithCancellation(GetReportAsync)
			.ToArrayAsync(ct);

		return new ProfitAndLossReports(accounts.ToImmutableDictionary(), [..reports]);

		async ValueTask<ProfitAndLossReport> GetReportAsync((LocalDate, Period) q, CancellationToken ct) {
			var (start, period) = q;

			var result = connection.QueryUnbufferedAsync<(int accountNumber, decimal balance)>(
				"""
				SELECT p.account_number, SUM(p.balance) 
				FROM profit_and_loss_daily p
				WHERE p.date 
					BETWEEN @start AND @end
				GROUP BY p.account_number 
				""", new {
					start = start.ToDateTimeUnspecified(),
					end = (start + period - Period.FromDays(1)).ToDateTimeUnspecified()
				});

			return await result.AggregateAsync(ProfitAndLossReport.For(start, period), (report, item) => report with {
				AccountBalances = report.AccountBalances.Add(new(item.accountNumber, item.balance / 100m))
			}, cancellationToken: ct);
		}
	}
}
