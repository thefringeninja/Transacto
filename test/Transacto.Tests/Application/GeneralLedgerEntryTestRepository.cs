using Transacto.Domain;
using Transacto.Framework;
using Transacto.Testing;

namespace Transacto.Application;

internal class GeneralLedgerEntryTestRepository(IFactRecorder facts) : IGeneralLedgerEntryRepository {
	private readonly FactRecorderRepository<GeneralLedgerEntry> _inner = new(facts);

	public async ValueTask<GeneralLedgerEntry> Get(GeneralLedgerEntryIdentifier identifier,
		CancellationToken cancellationToken = default) {
		var optional = await GetOptional(identifier, cancellationToken);

		return optional switch {
			Some<GeneralLedgerEntry> { Value: var value } => value,
			None => throw new GeneralLedgerEntryNotFoundException(identifier)
		};
	}

	public async ValueTask<Option<GeneralLedgerEntry>> GetOptional(GeneralLedgerEntryIdentifier identifier,
		CancellationToken cancellationToken = default) =>
		await _inner.GetOptional(GeneralLedgerEntry.FormatStreamIdentifier(identifier),
			cancellationToken);

	public void Add(GeneralLedgerEntry generalLedgerEntry) => _inner.Add(generalLedgerEntry);
}
