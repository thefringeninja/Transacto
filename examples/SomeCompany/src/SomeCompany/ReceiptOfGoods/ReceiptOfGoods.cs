using System.Collections.Immutable;
using Transacto.Domain;

namespace SomeCompany.ReceiptOfGoods;

partial record ReceiptOfGoods : IBusinessTransaction {
	
	public GeneralLedgerEntrySequenceNumber? SequenceNumber { get; set; }
	public static GeneralLedgerEntryNumberPrefix Prefix { get; } = new("receiptOfGoods");

	public GeneralLedgerEntryNumber? ReferenceNumber =>
		SequenceNumber.HasValue ? new GeneralLedgerEntryNumber(Prefix, SequenceNumber.Value) : null;

	public IEnumerable<TransactionItem> GetTransactionItems(ChartOfAccounts chartOfAccounts) {
		var (inventoryInTransit, inventoryOnHand) = Aggregate();

		yield return inventoryInTransit;
		yield return inventoryOnHand;
	}

	private (Credit inventoryInTransit, Debit inventoryOnHand) Aggregate() => ReceiptOfGoodsItems.Aggregate(
		(new Credit(new AccountNumber(1400)), new Debit(new AccountNumber(1450))),
		Accumulate);

	private static (Credit, Debit) Accumulate(
		(Credit inventoryInTransit, Debit inventoryOnHand) accounts, ReceiptOfGoodsItem item) =>
		(accounts.inventoryInTransit + item.Total, accounts.inventoryOnHand + item.Total);
}
