using Transacto.Domain;

namespace Transacto.Integration;

internal static class BusinessTransactionExtensions {
	public static JournalEntry ToJournalEntry(this IBusinessTransaction businessTransaction,
		ChartOfAccounts chartOfAccounts) {
		return businessTransaction switch {
			JournalEntry entry => entry,
			_ => new JournalEntry {
				Items = [..Items(businessTransaction)]
			}
		};

		IEnumerable<JournalEntry.Item> Items(IBusinessTransaction businessTransaction) {
			foreach (var t in businessTransaction.GetTransactionItems(chartOfAccounts)) {
				if (t is Debit debit) {
					yield return new JournalEntry.Item(debit.AccountNumber.Value, debit.Amount.ToDecimal(),
						JournalEntry.Type.Debit);
				} else if (t is Credit credit) {
					yield return new JournalEntry.Item(credit.AccountNumber.Value, credit.Amount.ToDecimal(),
						JournalEntry.Type.Credit);
				}
			}
		}
	}
}
