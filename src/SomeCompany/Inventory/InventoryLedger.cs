using System;
using Projac.Npgsql;
using SomeCompany.Framework;
using SomeCompany.Framework.Projections;
using SomeCompany.Infrastructure;
using SomeCompany.PurchaseOrders;
using SomeCompany.ReceiptOfGoods;

namespace SomeCompany.Inventory {
    public class InventoryLedger : NpgsqlProjectionBase {
        public InventoryLedger(string schema = "dbo") : base(new Scripts()) {
            When<CreateSchema>();

            When<InventoryItemDefined>(e => new[] {
                Sql.UniqueIdentifier(e.InventoryItemId)
                    .ToDbParameter("inventory_item_id"),
                Sql.VarChar(e.Sku, new NpgsqlVarCharSize(256))
                    .ToDbParameter("sku")
            });

            When<PurchaseOrderPlaced>(e => Array.ConvertAll(e.Items, item => new[] {
                Sql.UniqueIdentifier(item.InventoryItemId)
                    .ToDbParameter("inventory_item_id"),
                Sql.Decimal(item.Quantity)
                    .ToDbParameter("quantity")
            }));

            When<GoodsReceived>(e => Array.ConvertAll(e.Items, item => new[] {
                Sql.UniqueIdentifier(item.InventoryItemId)
                    .ToDbParameter("inventory_item_id"),
                Sql.Decimal(item.Quantity)
                    .ToDbParameter("quantity")
            }));
        }
    }
}
