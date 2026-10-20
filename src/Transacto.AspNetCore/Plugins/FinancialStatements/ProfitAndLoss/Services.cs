using System.Data.SQLite;
using Dapper;
using NodaTime;

namespace Transacto.Plugins.FinancialStatements.ProfitAndLoss;

internal static class Services {
	public static async Task<ProfitAndLossReports> ProfitAndLossReportsAsync(Func<SQLiteConnection> connectionFactory,
		IEnumerable<(LocalDate startingon, Period period)> reportRanges, CancellationToken ct) {
		await using var connection = connectionFactory();

		var reports = await reportRanges.ToAsyncEnumerable()
			.OrderBy(q => q.startingon)
			.Select(GetReportAsync)
			.ToArrayAsync(ct);

		return new ProfitAndLossReports([..reports]);

		async ValueTask<ReportGroup> GetReportAsync((LocalDate startingOn, Period period) q,
			CancellationToken ct) {
			var (start, period) = q;

			var result = connection.QueryUnbufferedAsync<(int number, string name, decimal balance)>(
				"""
				SELECT p.account_number, a.account_name, SUM(p.balance) 
				FROM profit_and_loss_daily p
					LEFT JOIN account_names a ON p.account_number = a.account_number
				WHERE p.date 
					BETWEEN @start AND @end
				GROUP BY p.account_number 
				""", new {
					start = start.ToDateTimeUnspecified(),
					end = (start + period - Period.FromDays(1)).ToDateTimeUnspecified()
				});

			var lines = await result.Select(Report.Line.Create).ToArrayAsync(cancellationToken: ct);

			return ReportGroup.Create(new ReportRange(q.startingOn, q.period), [..lines]);
		}
	}
}
