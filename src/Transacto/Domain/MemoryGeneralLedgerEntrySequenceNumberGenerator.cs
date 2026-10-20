using System.Collections.Concurrent;

namespace Transacto.Domain;

public class MemoryGeneralLedgerEntrySequenceNumberGenerator(int startingFrom = 1)
	: IGeneralLedgerEntrySequenceNumberGenerator {
	public static readonly MemoryGeneralLedgerEntrySequenceNumberGenerator Default = new();

	public static Func<Type, IGeneralLedgerEntrySequenceNumberGenerator> Factory {
		get {
			return field ??= Get();

			Func<Type, MemoryGeneralLedgerEntrySequenceNumberGenerator> Get() {
				ConcurrentDictionary<Type, MemoryGeneralLedgerEntrySequenceNumberGenerator> cache = [];

				return type => cache.GetOrAdd(type, _ => new MemoryGeneralLedgerEntrySequenceNumberGenerator());
			}
		}
	}
	
	public ValueTask<GeneralLedgerEntrySequenceNumber> NextAsync(
		GeneralLedgerEntry generalLedgerEntry,
		CancellationToken cancellationToken = default) => new(new GeneralLedgerEntrySequenceNumber(startingFrom++));
}
