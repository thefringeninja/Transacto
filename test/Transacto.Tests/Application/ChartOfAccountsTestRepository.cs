using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Testing;

namespace Transacto.Application {
    internal class ChartOfAccountsTestRepository : IChartOfAccountsRepository {
        private readonly IFactRecorder _factRecorder;

        public ChartOfAccountsTestRepository(IFactRecorder factRecorder) {
            _factRecorder = factRecorder;
        }

        public ValueTask<Optional<ChartOfAccounts>> GetOptional(CancellationToken cancellationToken = default) {
            var facts = _factRecorder.GetFacts().Where(x => x.Identifier == string.Empty).ToArray();

            if (facts.Length == 0) return default;

            var chartOfAccounts = ChartOfAccounts.Factory();
            chartOfAccounts.LoadFromHistory(facts.Select(x => x.Event));
            _factRecorder.Record(string.Empty, chartOfAccounts);
            return new ValueTask<Optional<ChartOfAccounts>>(chartOfAccounts);
        }

        public async ValueTask<ChartOfAccounts> Get(CancellationToken cancellationToken = default) {
            var optional = await GetOptional(cancellationToken);
            if (!optional.HasValue) {
                throw new InvalidOperationException();
            }

            return optional.Value;
        }

        public void Add(ChartOfAccounts chartOfAccounts) =>
            _factRecorder.Record(string.Empty, chartOfAccounts.GetChanges());
    }
}
