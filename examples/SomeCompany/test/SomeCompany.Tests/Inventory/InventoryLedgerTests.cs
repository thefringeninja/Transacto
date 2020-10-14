using System;
using System.Threading.Tasks;
using SomeCompany.Infrastructure;
using SomeCompany.PurchaseOrders;
using SomeCompany.ReceiptOfGoods;
using Xunit;

namespace SomeCompany.Inventory {
    public class InventoryLedgerTests {
        private readonly string _schema;

        public InventoryLedgerTests() {
            _schema = $"dbo{Guid.NewGuid():n}";
        }

        [Theory, AutoSomeCompanyData]
        public Task when_a_purchase_order_is_placed(InventoryItem[] inventoryItems) =>
            new NpgsqlProjectionScenario(new InventoryLedger(_schema))
                .Given(new CreateSchema())
                .Given(Array.ConvertAll(inventoryItems, item => (object)new InventoryItemDefined {
                    InventoryItemId = item.InventoryItemId,
                    Sku = item.Sku
                }))
                .Given(new PurchaseOrder {
                    PurchaseOrderItems = Array.ConvertAll(inventoryItems, ConverToPurchaseOrderItem)
                })
                .Then(_schema, "inventory_ledger", Array.ConvertAll(inventoryItems, item => new InventoryLedgerItem {
                    InventoryItemId = item.InventoryItemId,
                    Sku = item.Sku,
                    OnOrder = item.Quantity
                }))
                .Assert();

        private static PurchaseOrderItem ConverToPurchaseOrderItem(InventoryItem item) {
            return new PurchaseOrderItem {
                ItemId = item.InventoryItemId.ToString(),
                Quantity = Convert.ToDouble(item.Quantity),
                UnitPrice = Convert.ToDouble(item.UnitPrice)
            };
        }

        [Theory, AutoSomeCompanyData]
        public Task when_goods_are_received(InventoryItem[] inventoryItems) =>
            new NpgsqlProjectionScenario(new InventoryLedger(_schema))
                .Given(new CreateSchema())
                .Given(Array.ConvertAll(inventoryItems, item => (object)new InventoryItemDefined {
                    InventoryItemId = item.InventoryItemId,
                    Sku = item.Sku
                }))
                .Given(new PurchaseOrder {
                    PurchaseOrderItems = Array.ConvertAll(inventoryItems, ConverToPurchaseOrderItem)
                }, new GoodsReceived {
                    Items = Array.ConvertAll(inventoryItems, item => new GoodsReceived.ReceiptItem {
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        InventoryItemId = item.InventoryItemId
                    })
                })
                .Then(_schema, "inventory_ledger", Array.ConvertAll(inventoryItems, item => new InventoryLedgerItem {
                    InventoryItemId = item.InventoryItemId,
                    Sku = item.Sku,
                    OnHand = item.Quantity
                }))
                .Assert();

        public class InventoryItem {
            public string Sku { get; set; }
            public Guid InventoryItemId { get; set; }
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
        }
    }
}
