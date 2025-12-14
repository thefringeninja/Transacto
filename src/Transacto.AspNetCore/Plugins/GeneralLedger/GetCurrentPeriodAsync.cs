using Transacto.Domain;

namespace Transacto.Plugins.GeneralLedger;

public delegate ValueTask<AccountingPeriod?> GetCurrentPeriodAsync(CancellationToken ct);
