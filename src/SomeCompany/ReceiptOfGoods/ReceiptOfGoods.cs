using System;
using System.Collections.Generic;
using System.Linq;
using Transacto.Domain;

namespace SomeCompany.ReceiptOfGoods {
	partial class ReceiptOfGoods : IBusinessTransaction {
		private static (Credit, Debit) Accumulate((Credit, Debit) _, ReceiptOfGoodsItem item) {
			var (inventoryInTransit, inventoryOnHand) = _;
			return (inventoryInTransit + item.Total, inventoryOnHand + item.Total);
		}

		public GeneralLedgerEntryNumber ReferenceNumber =>
			new GeneralLedgerEntryNumber("goodsReceipt", ReceiptOfGoodsNumber);

		public void Apply(GeneralLedgerEntry entry, ChartOfAccounts chartOfAccounts) {
			var (inventoryInTransit, inventoryOnHand) = ReceiptOfGoodsItems.Aggregate(
				(new Credit(new AccountNumber(1400)), new Debit(new AccountNumber(1450))),
				Accumulate);

			entry.ApplyCredit(inventoryInTransit, chartOfAccounts);
			entry.ApplyDebit(inventoryOnHand, chartOfAccounts);
			entry.ApplyTransaction(this);
		}

		public IEnumerable<object> GetAdditionalChanges() {
			yield return new GoodsReceived {
				ReceiptId = ReceiptOfGoodsId,
				ReceiptNumber = ReceiptOfGoodsNumber,
				Items = ReceiptOfGoodsItems.Select(item => new GoodsReceived.ReceiptItem {
					Quantity = Convert.ToDecimal(item.Quantity),
					InventoryItemId = item.ItemId,
					UnitPrice = Convert.ToDecimal(item.UnitPrice)
				}).ToArray()
			};
		}

		public int? Version { get; set; }
	}
}
