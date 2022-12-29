using System.Collections.Immutable;
using Transacto.Domain;

namespace SomeCompany.PurchaseOrders;

public partial record PurchaseOrder : IBusinessTransaction {
	public GeneralLedgerEntrySequenceNumber SequenceNumber => new(PurchaseOrderNumber);

	public IEnumerable<object> GetTransactionItems() {
		var (accountsPayable, inventoryInTransit) = Aggregate();

		yield return accountsPayable;
		yield return inventoryInTransit;
		yield return new PurchaseOrderPlaced {
			PurchaseOrderId = PurchaseOrderId,
			VendorId = VendorId,
			PurchaseOrderNumber = PurchaseOrderNumber,
			Items = ImmutableArray.CreateRange(PurchaseOrderItems,
				x => new PurchaseOrderPlaced.PurchaseOrderItem {
					Quantity = (decimal)x.Quantity,
					UnitPrice = (decimal)x.UnitPrice,
					InventoryItemId = x.ItemId
				})
		};
	}

	private static (Credit, Debit) Accumulate(
		(Credit accountsPayable, Debit inventoryInTransit) accounts, PurchaseOrderItem item) =>
		(accounts.accountsPayable + item.Total, accounts.inventoryInTransit + item.Total);

	private (Credit accountsPayable, Debit inventoryInTransit) Aggregate() => PurchaseOrderItems.Aggregate(
		(new Credit(new AccountNumber(2150)), new Debit(new AccountNumber(1400))),
		Accumulate);
}
