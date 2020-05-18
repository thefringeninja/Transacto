using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Transacto.Domain;
using Transacto.Framework;

namespace Transacto.Infrastructure {
    public class GeneralLedgerEventStoreRepository : IGeneralLedgerRepository {
        private readonly EventStoreRepository<GeneralLedger, string> _inner;

        public GeneralLedgerEventStoreRepository(EventStoreClient eventStore,
            IMessageTypeMapper messageTypeMapper, UnitOfWork unitOfWork) {
            _inner = new EventStoreRepository<GeneralLedger, string>(eventStore, unitOfWork,
                GeneralLedger.Factory, _ => nameof(GeneralLedger),
                identifier => identifier, messageTypeMapper);
        }

        public async ValueTask<GeneralLedger> Get(CancellationToken cancellationToken = default) {
	        var generalLedger = await _inner.GetById(nameof(GeneralLedger), cancellationToken);
	        if (!generalLedger.HasValue) {
				generalLedger = new GeneralLedger(PeriodIdentifier.FromClock(() => DateTimeOffset.UtcNow));
	        }

	        return generalLedger.Value;
        }
    }
}
