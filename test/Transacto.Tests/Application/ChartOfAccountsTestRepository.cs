using Transacto.Domain;
using Transacto.Framework;
using Transacto.Testing;

namespace Transacto.Application;

internal class ChartOfAccountsTestRepository : IChartOfAccountsRepository {
	private readonly FactRecorderRepository<ChartOfAccounts> _inner;

	public ChartOfAccountsTestRepository(IFactRecorder factRecorder) {
		_inner = new FactRecorderRepository<ChartOfAccounts>(factRecorder);
	}

	public ValueTask<Optional<ChartOfAccounts>> GetOptional(CancellationToken cancellationToken = default)
		=> _inner.GetOptional(ChartOfAccounts.Identifier, cancellationToken);

	public async ValueTask<ChartOfAccounts> Get(CancellationToken cancellationToken = default) =>
		await GetOptional(cancellationToken) switch {
			{ HasValue: true } optional => optional.Value,
			_ => throw new ChartOfAccountsNotFoundException()
		};

	public void Add(ChartOfAccounts chartOfAccounts) => _inner.Add(chartOfAccounts);
}
