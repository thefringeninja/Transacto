using EventStore.Client;
using Transacto.Domain;
using Transacto.Framework;

namespace Transacto.Infrastructure.EventStore;

public class GeneralLedgerEntryEventStoreRepository : IGeneralLedgerEntryRepository {
	private readonly EventStoreRepository<GeneralLedgerEntry> _inner;

	public GeneralLedgerEntryEventStoreRepository(EventStoreClient eventStore,
		IMessageTypeMapper messageTypeMapper) {
		_inner = new EventStoreRepository<GeneralLedgerEntry>(eventStore,
			GeneralLedgerEntry.Factory, messageTypeMapper);
	}

	public async ValueTask<GeneralLedgerEntry> Get(GeneralLedgerEntryIdentifier identifier,
		CancellationToken cancellationToken = default) =>
		await _inner.GetById(GeneralLedgerEntry.FormatStreamIdentifier(identifier), cancellationToken) switch {
			{ HasValue: true } optional => optional.Value,
			_ => throw new GeneralLedgerEntryNotFoundException(identifier)
		};

	public void Add(GeneralLedgerEntry generalLedgerEntry) => _inner.Add(generalLedgerEntry);
}
