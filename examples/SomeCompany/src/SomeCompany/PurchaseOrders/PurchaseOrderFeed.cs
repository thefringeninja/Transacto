using System.Collections.Immutable;
using Transacto.Framework;
using Transacto.Infrastructure.SqlStreamStore;

namespace SomeCompany.PurchaseOrders;

public class PurchaseOrderFeed : StreamStoreFeedProjection<PurchaseOrderFeedEntry> {
	public PurchaseOrderFeed(IMessageTypeMapper messageTypeMapper) : base("purchaseOrders", messageTypeMapper) {
		When<PurchaseOrderPlaced>((e, _) => new PurchaseOrderFeedEntry {
			PurchaseOrderId = e.PurchaseOrderId,
			VendorId = e.VendorId,
			PurchaseOrderNumber = e.PurchaseOrderNumber,
			Events = ImmutableArray<string>.Empty.Add("purchaseOrderPlaced"),
			Items = ImmutableArray.CreateRange(e.Items, item => new PurchaseOrderFeedEntryItem {
				InventoryItemId = item.InventoryItemId,
				Quantity = Convert.ToDouble(item.Quantity),
				UnitPrice = Convert.ToDouble(item.UnitPrice)
			})
		});
	}
}
