using System.Collections.Immutable;
using Transacto.Domain;

namespace SomeCompany.PurchaseOrders;

public partial record PurchaseOrder : IBusinessTransaction {
	public GeneralLedgerEntrySequenceNumber? SequenceNumber { get; set; }
	public static GeneralLedgerEntryNumberPrefix Prefix { get; } = new("purchaseOrder");

	public GeneralLedgerEntryNumber? ReferenceNumber =>
		SequenceNumber.HasValue ? new GeneralLedgerEntryNumber(Prefix, SequenceNumber.Value) : null;

	public IEnumerable<TransactionItem> GetTransactionItems(ChartOfAccounts chartOfAccounts) {
		var (accountsPayable, inventoryInTransit) = Aggregate();

		yield return accountsPayable;
		yield return inventoryInTransit;
	}

	private static (Credit, Debit) Accumulate(
		(Credit accountsPayable, Debit inventoryInTransit) accounts, PurchaseOrderItem item) =>
		(accounts.accountsPayable + item.Total, accounts.inventoryInTransit + item.Total);

	private (Credit accountsPayable, Debit inventoryInTransit) Aggregate() => PurchaseOrderItems.Aggregate(
		(new Credit(new AccountNumber(2100)), new Debit(new AccountNumber(1200))),
		Accumulate);
}
