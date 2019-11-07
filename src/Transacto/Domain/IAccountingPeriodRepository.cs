using System.Threading;
using System.Threading.Tasks;

namespace Transacto.Domain {
    public interface IAccountingPeriodRepository {
        ValueTask<AccountingPeriod> Get(PeriodIdentifier period, CancellationToken cancellationToken = default);
        void Add(AccountingPeriod accountingPeriod);
    }
}
