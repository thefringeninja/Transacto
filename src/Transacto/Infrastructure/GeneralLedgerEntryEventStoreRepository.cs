using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Grpc;
using Transacto.Domain;
using Transacto.Framework;

namespace Transacto.Infrastructure {
    public class GeneralLedgerEntryEventStoreRepository : IGeneralLedgerEntryRepository {
        private readonly EventStoreRepository<GeneralLedgerEntry, GeneralLedgerEntryIdentifier> _inner;

        public GeneralLedgerEntryEventStoreRepository(EventStoreGrpcClient eventStore, UnitOfWork unitOfWork) {
            _inner = new EventStoreRepository<GeneralLedgerEntry, GeneralLedgerEntryIdentifier>(eventStore, unitOfWork,
                GeneralLedgerEntry.Factory, period => period.GeneralLedgerEntryIdentifier,
                identifier => $"generalLedgerEntry-{identifier.ToString()}");
        }

        public async ValueTask<GeneralLedgerEntry> Get(GeneralLedgerEntryIdentifier identifier,
            CancellationToken cancellationToken = default) {
            var optionalGeneralLedgerEntry = await _inner.GetById(identifier, cancellationToken);
            if (!optionalGeneralLedgerEntry.HasValue) {
                throw new InvalidOperationException();
            }

            return optionalGeneralLedgerEntry.Value;
        }

        public void Add(GeneralLedgerEntry generalLedgerEntry) => _inner.Add(generalLedgerEntry);
    }
}
