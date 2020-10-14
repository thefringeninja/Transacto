using System.Threading;
using System.Threading.Tasks;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Testing;

namespace Transacto.Application {
	internal class ChartOfAccountsTestRepository : IChartOfAccountsRepository {
		private readonly FactRecorderRepository<ChartOfAccounts> _inner;

		public ChartOfAccountsTestRepository(IFactRecorder factRecorder) {
			_inner = new FactRecorderRepository<ChartOfAccounts>(factRecorder, ChartOfAccounts.Factory);
		}

		public ValueTask<Optional<ChartOfAccounts>> GetOptional(CancellationToken cancellationToken = default)
			=> _inner.GetOptional(ChartOfAccounts.Identifier, cancellationToken);

		public async ValueTask<ChartOfAccounts> Get(CancellationToken cancellationToken = default) {
			var optional = await GetOptional(cancellationToken);
			if (!optional.HasValue) {
				throw new ChartOfAccountsNotFoundException();
			}

			return optional.Value;
		}

		public void Add(ChartOfAccounts chartOfAccounts) => _inner.Add(chartOfAccounts);
	}
}
