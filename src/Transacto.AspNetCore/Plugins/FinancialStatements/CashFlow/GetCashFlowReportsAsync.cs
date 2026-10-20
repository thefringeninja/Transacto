using System.Collections.Immutable;
using NodaTime;

namespace Transacto.Plugins.FinancialStatements.CashFlow;

internal delegate Task<CashFlowReports> GetCashFlowReportsAsync(
	ImmutableArray<(LocalDate, Period)> reportRanges, CancellationToken ct);
