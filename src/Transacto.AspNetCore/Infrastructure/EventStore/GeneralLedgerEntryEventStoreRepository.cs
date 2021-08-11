using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Transacto.Domain;
using Transacto.Framework;

namespace Transacto.Infrastructure.EventStore {
	public class GeneralLedgerEntryEventStoreRepository : IGeneralLedgerEntryRepository {
		private readonly EventStoreRepository<GeneralLedgerEntry> _inner;

		public GeneralLedgerEntryEventStoreRepository(EventStoreClient eventStore,
			IMessageTypeMapper messageTypeMapper) {
			_inner = new EventStoreRepository<GeneralLedgerEntry>(eventStore,
				GeneralLedgerEntry.Factory, messageTypeMapper);
		}

		public async ValueTask<GeneralLedgerEntry> Get(GeneralLedgerEntryIdentifier identifier,
			CancellationToken cancellationToken = default) {
			var optionalGeneralLedgerEntry = await _inner.GetById(GeneralLedgerEntry.FormatStreamIdentifier(identifier),
				cancellationToken);
			if (!optionalGeneralLedgerEntry.HasValue) {
				throw new GeneralLedgerEntryNotFoundException(identifier);
			}

			return optionalGeneralLedgerEntry.Value;
		}

		public void Add(GeneralLedgerEntry generalLedgerEntry) => _inner.Add(generalLedgerEntry);
	}
}
