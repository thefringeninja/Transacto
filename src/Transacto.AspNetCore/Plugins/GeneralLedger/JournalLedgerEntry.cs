using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Transacto.Domain;

namespace Transacto.Plugins.GeneralLedger;

public record JournalLedgerEntry(
	string ReferenceNumber,
	ImmutableArray<JournalLedgerEntry.Item> Items) {

	public static JournalLedgerEntry FromGeneralLedgerEntry(GeneralLedgerEntry entry) {
		var items = entry.Debits.Select(d => new Item(d.AccountNumber.ToInt32(), d.Amount.ToDecimal(), Type.Debit))
			.Concat(
				entry.Credits.Select(c => new Item(c.AccountNumber.ToInt32(), c.Amount.ToDecimal(), Type.Credit)))
			.OrderBy(i => i.AccountNumber)
			.ToImmutableArray();

		return new JournalLedgerEntry(entry.ReferenceNumber.ToString(), items);
	}

	public enum Type { Debit, Credit };

	public record Item(int AccountNumber, decimal Amount, Type Type);
}
