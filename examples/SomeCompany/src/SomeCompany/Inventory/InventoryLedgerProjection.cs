using Projac.Npgsql;
using SomeCompany.PurchaseOrders;
using SomeCompany.ReceiptOfGoods;
using Transacto.Framework.Projections;
using Transacto.Infrastructure.Npgsql;

namespace SomeCompany.Inventory;

public class InventoryLedgerProjection : NpgsqlProjection {
	public InventoryLedgerProjection() : base(new Scripts()) {
		When<CreateSchema>();

		When<InventoryItemDefined>(e => new[] {
			Sql.Parameter(() => e.InventoryItemId),
			Sql.Parameter(() => e.Sku)
		});

		When<PurchaseOrderPlaced>(e => e.Items.Select(item => new[] {
			Sql.Parameter(() => item.InventoryItemId),
			Sql.Parameter(() => item.Quantity)
		}).ToArray());

		When<GoodsReceived>(e => e.Items.Select(item => new[] {
			Sql.Parameter(() => item.InventoryItemId),
			Sql.Parameter(() => item.Quantity)
		}).ToArray());
	}
}
