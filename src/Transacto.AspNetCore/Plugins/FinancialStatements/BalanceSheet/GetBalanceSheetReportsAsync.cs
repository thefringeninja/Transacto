using System.Collections.Immutable;
using NodaTime;

namespace Transacto.Plugins.FinancialStatements.BalanceSheet;

internal delegate Task<BalanceSheetReports> GetBalanceSheetReportsAsync(
	ImmutableArray<LocalDate> thru,
	CancellationToken ct);
