using Transacto.Domain;
using Transacto.Testing;

namespace Transacto.Application; 

internal class GeneralLedgerEntryTestRepository : IGeneralLedgerEntryRepository {
	private readonly FactRecorderRepository<GeneralLedgerEntry> _inner;

	public GeneralLedgerEntryTestRepository(IFactRecorder facts) {
		_inner = new FactRecorderRepository<GeneralLedgerEntry>(facts, GeneralLedgerEntry.Factory);
	}

	public async ValueTask<GeneralLedgerEntry> Get(GeneralLedgerEntryIdentifier identifier,
		CancellationToken cancellationToken = default) {
		var optional = await _inner.GetOptional(GeneralLedgerEntry.FormatStreamIdentifier(identifier),
			cancellationToken);
		return optional.HasValue ? optional.Value : throw new GeneralLedgerEntryNotFoundException(identifier);
	}

	public void Add(GeneralLedgerEntry generalLedgerEntry) => _inner.Add(generalLedgerEntry);
}
