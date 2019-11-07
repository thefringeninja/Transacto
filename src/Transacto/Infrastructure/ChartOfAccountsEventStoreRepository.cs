using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Grpc;
using Transacto.Domain;
using Transacto.Framework;

namespace Transacto.Infrastructure {
    public class ChartOfAccountsEventStoreRepository : IChartOfAccountsRepository {
        private readonly EventStoreRepository<ChartOfAccounts, string> _inner;

        public ChartOfAccountsEventStoreRepository(EventStoreGrpcClient eventStore, UnitOfWork unitOfWork) {
            _inner = new EventStoreRepository<ChartOfAccounts, string>(eventStore, unitOfWork,
                ChartOfAccounts.Factory, _ => string.Empty, _ => "chartOfAccounts");
        }

        public ValueTask<Optional<ChartOfAccounts>> GetOptional(CancellationToken cancellationToken = default)
            => _inner.GetById(string.Empty, cancellationToken);

        public async ValueTask<ChartOfAccounts> Get(CancellationToken cancellationToken = default) {
            var optionalChartOfAccounts = await GetOptional(cancellationToken);
            if (!optionalChartOfAccounts.HasValue) {
                throw new InvalidOperationException();
            }

            return optionalChartOfAccounts.Value;
        }

        public void Add(ChartOfAccounts chartOfAccounts) => _inner.Add(chartOfAccounts);
    }
}
