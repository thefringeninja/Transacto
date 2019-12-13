using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Grpc;
using Transacto.Domain;
using Transacto.Framework;

namespace Transacto.Infrastructure {
    public class AccountingPeriodEventStoreRepository : IAccountingPeriodRepository {
        private readonly EventStoreRepository<AccountingPeriod, PeriodIdentifier> _inner;

        public AccountingPeriodEventStoreRepository(EventStoreGrpcClient eventStore,
            IMessageTypeMapper messageTypeMapper, UnitOfWork unitOfWork) {
            _inner = new EventStoreRepository<AccountingPeriod, PeriodIdentifier>(eventStore, unitOfWork,
                AccountingPeriod.Factory, period => period.Period,
                identifier => $"accountingPeriod-{identifier.ToString()}", messageTypeMapper);
        }

        public async ValueTask<AccountingPeriod> Get(PeriodIdentifier period,
            CancellationToken cancellationToken = default) {
            var optionalAccountingPeriod = await _inner.GetById(period, cancellationToken);
            if (!optionalAccountingPeriod.HasValue) {
                throw new InvalidOperationException();
            }

            return optionalAccountingPeriod.Value;
        }

        public void Add(AccountingPeriod accountingPeriod) => _inner.Add(accountingPeriod);
    }
}
