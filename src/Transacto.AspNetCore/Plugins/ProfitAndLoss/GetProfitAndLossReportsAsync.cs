using NodaTime;

namespace Transacto.Plugins.ProfitAndLoss;

public delegate Task<ProfitAndLossReports> GetProfitAndLossReportsAsync(
	IEnumerable<(LocalDate startingon, Period period)> reportRanges, CancellationToken ct);
