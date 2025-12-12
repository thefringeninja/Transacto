namespace Transacto.Domain;

public interface IGeneralLedgerEntrySequenceNumberGenerator {
	public ValueTask<GeneralLedgerEntrySequenceNumber> NextAsync(
		GeneralLedgerEntry generalLedgerEntry,
		CancellationToken cancellationToken = default);
}
