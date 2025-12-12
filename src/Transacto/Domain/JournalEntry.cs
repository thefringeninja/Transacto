using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Transacto.Domain;

public partial class JournalEntry : IBusinessTransaction {
	public string? Description { get; init; }
	public ImmutableArray<Item> Items { get; init; } = ImmutableArray<Item>.Empty;
	public GeneralLedgerEntrySequenceNumber? SequenceNumber { get; init; }
	public static GeneralLedgerEntryNumberPrefix Prefix { get; } = new("journalEntry");
	public GeneralLedgerEntryNumber? ReferenceNumber =>
		SequenceNumber.HasValue ? new GeneralLedgerEntryNumber(Prefix, SequenceNumber.Value) : null;

	public IEnumerable<object> GetTransactionItems() {
		foreach (var (accountNumber, amount, type) in Items.OrderBy(item => item.AccountNumber)) {
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
