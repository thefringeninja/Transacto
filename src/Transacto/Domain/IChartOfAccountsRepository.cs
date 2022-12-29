using Transacto.Framework;

namespace Transacto.Domain;

public interface IChartOfAccountsRepository {
	ValueTask<Optional<ChartOfAccounts>> GetOptional(CancellationToken cancellationToken = default);
	ValueTask<ChartOfAccounts> Get(CancellationToken cancellationToken = default);
	void Add(ChartOfAccounts chartOfAccounts);
}
