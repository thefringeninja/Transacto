using System;
using System.Collections.Generic;
using System.Linq;
using Transacto.Domain;

namespace SomeCompany.PurchaseOrders {
	public partial class PurchaseOrder : IBusinessTransaction {
		public GeneralLedgerEntryNumber ReferenceNumber => new GeneralLedgerEntryNumber();

		public void Apply(GeneralLedgerEntry generalLedgerEntry, ChartOfAccounts chartOfAccounts) {
			var (accountsPayable, inventoryInTransit) =
				PurchaseOrderItems.Aggregate((new Credit(new AccountNumber(2150)), new Debit(new AccountNumber(1400))),
					Accumulate);

			generalLedgerEntry.ApplyCredit(accountsPayable, chartOfAccounts);
			generalLedgerEntry.ApplyDebit(inventoryInTransit, chartOfAccounts);
			generalLedgerEntry.ApplyTransaction(this);
		}

		private static (Credit, Debit) Accumulate((Credit, Debit) _, PurchaseOrderItem item) {
			var (accountsPayable, inventoryInTransit) = _;
			return (accountsPayable + item.Total, inventoryInTransit + item.Total);
		}

		public IEnumerable<object> GetAdditionalChanges() {
			yield return new PurchaseOrderPlaced {
				PurchaseOrderId = PurchaseOrderId,
				VendorId = VendorId,
				PurchaseOrderNumber = PurchaseOrderNumber,
				Items = PurchaseOrderItems
					.Select(x => new PurchaseOrderPlaced.PurchaseOrderItem {
						Quantity = (decimal)x.Quantity,
						UnitPrice = (decimal)x.UnitPrice,
						InventoryItemId = x.ItemId
					}).ToArray()
			};
		}

		public int? Version { get; set; }
	}
}
