namespace Transacto.Domain;

public interface ISequenceNumberGenerator {
	public ValueTask<GeneralLedgerEntrySequenceNumber> NextAsync(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
		CancellationToken cancellationToken = default);
}

public class MemorySequenceNumberGenerator(int startingFrom = 0) : ISequenceNumberGenerator {
	public static readonly MemorySequenceNumberGenerator Default = new();

	public ValueTask<GeneralLedgerEntrySequenceNumber> NextAsync(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
		CancellationToken cancellationToken = default) => new(new GeneralLedgerEntrySequenceNumber(startingFrom++));
}

public delegate ISequenceNumberGenerator GetSequenceNumbers(IBusinessTransaction businessTransaction);
