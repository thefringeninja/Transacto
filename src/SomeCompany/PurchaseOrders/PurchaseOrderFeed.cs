using System;
using Transacto;
using Transacto.Framework;

namespace SomeCompany.PurchaseOrders {
	public class PurchaseOrderFeed : StreamStoreFeedProjection<PurchaseOrderFeedEntry> {
		public PurchaseOrderFeed(IMessageTypeMapper messageTypeMapper) : base("purchaseOrders", messageTypeMapper) {
			When<PurchaseOrderPlaced>((e, _) => new PurchaseOrderFeedEntry {
				PurchaseOrderId = e.PurchaseOrderId,
				VendorId = e.VendorId,
				PurchaseOrderNumber = e.PurchaseOrderNumber,
				Items = Array.ConvertAll(e.Items, item => new PurchaseOrderFeedEntry.Item {
					InventoryItemId = item.InventoryItemId,
					Quantity = item.Quantity,
					UnitPrice = item.UnitPrice
				})
			});
		}
	}
}
