using NodaTime;

namespace Transacto.Plugins.FinancialStatements.CashFlow;

public record ReportRange(LocalDate StartingOn, Period Period);
