using System.Collections.Immutable;
using Transacto.Domain;

namespace SomeCompany.ReceiptOfGoods;

partial record ReceiptOfGoods : IBusinessTransaction {
	public GeneralLedgerEntrySequenceNumber SequenceNumber => new(ReceiptOfGoodsNumber);

	public IEnumerable<object> GetTransactionItems() {
		var (inventoryInTransit, inventoryOnHand) = Aggregate();

		yield return inventoryInTransit;
		yield return inventoryOnHand;
		yield return new GoodsReceived {
			ReceiptId = ReceiptOfGoodsId,
			ReceiptNumber = ReceiptOfGoodsNumber,
			Items = ImmutableArray.CreateRange(ReceiptOfGoodsItems, item => new GoodsReceived.ReceiptItem {
				Quantity = Convert.ToDecimal(item.Quantity),
				InventoryItemId = item.ItemId,
				UnitPrice = Convert.ToDecimal(item.UnitPrice)
			})
		};
	}

	private (Credit inventoryInTransit, Debit inventoryOnHand) Aggregate() => ReceiptOfGoodsItems.Aggregate(
		(new Credit(new AccountNumber(1400)), new Debit(new AccountNumber(1450))),
		Accumulate);

	private static (Credit, Debit) Accumulate(
		(Credit inventoryInTransit, Debit inventoryOnHand) accounts, ReceiptOfGoodsItem item) =>
		(accounts.inventoryInTransit + item.Total, accounts.inventoryOnHand + item.Total);
}
