using System.Collections.Immutable;
using NodaTime;

namespace Transacto.Plugins.BalanceSheet;

public delegate Task<BalanceSheetReports> GetBalanceSheetReportsAsync(
	ImmutableArray<LocalDate> thru,
	CancellationToken ct);