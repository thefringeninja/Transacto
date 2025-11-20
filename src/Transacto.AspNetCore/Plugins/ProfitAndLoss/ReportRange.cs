using NodaTime;

namespace Transacto.Plugins.ProfitAndLoss;

public record ReportRange(LocalDate StartingOn, Period Period);
