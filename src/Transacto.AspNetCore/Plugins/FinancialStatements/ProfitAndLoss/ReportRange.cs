using NodaTime;

namespace Transacto.Plugins.FinancialStatements.ProfitAndLoss;

public record ReportRange(LocalDate StartingOn, Period Period);
