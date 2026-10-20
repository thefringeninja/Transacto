using System.Collections.Immutable;
using NodaTime;

namespace Transacto.Plugins.FinancialStatements.ProfitAndLoss;

public delegate Task<ProfitAndLossReports> GetProfitAndLossReportsAsync(
	ImmutableArray<(LocalDate startingon, Period period)> reportRanges, CancellationToken ct);
