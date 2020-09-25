using System;
using Projac.Npgsql;
using SomeCompany.PurchaseOrders;
using SomeCompany.ReceiptOfGoods;
using Transacto;
using Transacto.Framework.Projections;
using Transacto.Framework.Projections.Npgsql;

namespace SomeCompany.Inventory {
	public class InventoryLedgerProjection : NpgsqlProjection {
		public InventoryLedgerProjection() : base(new Scripts()) {
			When<CreateSchema>();

			When<InventoryItemDefined>(e => new[] {
				Sql.Parameter(() => e.InventoryItemId),
				Sql.Parameter(() => e.Sku)
			});

			When<PurchaseOrderPlaced>(e => Array.ConvertAll(e.Items, item => new[] {
				Sql.Parameter(() => item.InventoryItemId),
				Sql.Parameter(() => item.Quantity)
			}));

			When<GoodsReceived>(e => Array.ConvertAll(e.Items, item => new[] {
				Sql.Parameter(() => item.InventoryItemId),
				Sql.Parameter(() => item.Quantity)
			}));
		}
	}
}
