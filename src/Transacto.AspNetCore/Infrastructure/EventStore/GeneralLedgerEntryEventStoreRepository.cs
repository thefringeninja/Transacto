using KurrentDB.Client;
using Transacto.Domain;
using Transacto.Framework;

namespace Transacto.Infrastructure.EventStore;

public class GeneralLedgerEntryEventStoreRepository(
	KurrentDBClient eventStore,
	IMessageTypeMapper messageTypeMapper)
	: IGeneralLedgerEntryRepository {
	private readonly EventStoreRepository<GeneralLedgerEntry> _inner = new(eventStore, messageTypeMapper);

	public async ValueTask<GeneralLedgerEntry> Get(GeneralLedgerEntryIdentifier identifier,
		CancellationToken cancellationToken = default) =>
		await _inner.GetById(GeneralLedgerEntry.FormatStreamIdentifier(identifier), cancellationToken) switch {
			{ HasValue: true } optional => optional.Value,
			_ => throw new GeneralLedgerEntryNotFoundException(identifier)
		};

	public async ValueTask<Optional<GeneralLedgerEntry>> GetOptional(GeneralLedgerEntryIdentifier identifier,
		CancellationToken cancellationToken = default) {
		return await _inner.GetById(GeneralLedgerEntry.FormatStreamIdentifier(identifier), cancellationToken);
	}

	public void Add(GeneralLedgerEntry generalLedgerEntry) => _inner.Add(generalLedgerEntry);
}
