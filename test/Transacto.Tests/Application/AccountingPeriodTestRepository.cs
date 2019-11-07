using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Transacto.Domain;
using Transacto.Testing;

namespace Transacto.Application {
    internal class AccountingPeriodTestRepository : IAccountingPeriodRepository {
        private readonly IFactRecorder _factRecorder;

        public AccountingPeriodTestRepository(IFactRecorder factRecorder) {
            _factRecorder = factRecorder;
        }

        public ValueTask<AccountingPeriod> Get(PeriodIdentifier period, CancellationToken cancellationToken = default) {
            var accountingPeriod = AccountingPeriod.Factory();

            accountingPeriod.LoadFromHistory(_factRecorder.GetFacts().Where(x => x.Identifier == period.ToString())
                .Select(x => x.Event));

            _factRecorder.Record(accountingPeriod.Period.ToString(), accountingPeriod);

            return new ValueTask<AccountingPeriod>(accountingPeriod);
        }

        public void Add(AccountingPeriod accountingPeriod) =>
            _factRecorder.Record(accountingPeriod.Period.ToString(), accountingPeriod.GetChanges());
    }
}
