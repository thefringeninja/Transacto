using System.Collections.Immutable;
using Transacto.Framework;
using Transacto.Infrastructure.SqlStreamStore;

namespace SomeCompany.PurchaseOrders;

public class PurchaseOrderFeed : StreamStoreFeedProjection<PurchaseOrderFeedEntry> {
	public PurchaseOrderFeed(IMessageTypeMapper messageTypeMapper) : base("purchaseOrders", messageTypeMapper) {
		When<PurchaseOrder>(e => new PurchaseOrderFeedEntry {
			PurchaseOrderId = e.PurchaseOrderId,
			VendorId = e.VendorId,
			PurchaseOrderNumber = e.PurchaseOrderNumber,
			Events = ImmutableArray<string>.Empty.Add("purchaseOrderPlaced"),
			Items = ImmutableArray.CreateRange(e.PurchaseOrderItems, item => new PurchaseOrderFeedEntryItem {
				InventoryItemId = item.ItemId,
				Quantity = Convert.ToDouble(item.Quantity),
				UnitPrice = Convert.ToDouble(item.UnitPrice)
			})
		});
	}
}
