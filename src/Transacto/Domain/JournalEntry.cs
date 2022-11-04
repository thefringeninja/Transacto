using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Transacto.Domain; 

public partial class JournalEntry : IBusinessTransaction {
	public GeneralLedgerEntrySequenceNumber SequenceNumber => new(JournalEntryNumber);
	public int JournalEntryNumber { get; init; }
	public string Description { get; init; } = null!;
	public ImmutableArray<Item> Items { get; init; } = ImmutableArray<Item>.Empty;

	public IEnumerable<object> GetTransactionItems() {
		foreach (var (accountNumber, amount, type) in Items) {
			yield return type switch {
				Type.Credit => new Credit(new AccountNumber(accountNumber), new Money(amount)),
				Type.Debit => new Debit(new AccountNumber(accountNumber), new Money(amount)),
				_ => throw new ArgumentOutOfRangeException(nameof(type))
			};
		}
	}

	public record Item(int AccountNumber, decimal Amount, Type Type);

	public enum Type { Credit, Debit }
}