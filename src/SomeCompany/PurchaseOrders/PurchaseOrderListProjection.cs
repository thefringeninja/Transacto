using System;
using SomeCompany.Framework;
using SomeCompany.Infrastructure;

namespace SomeCompany.PurchaseOrders {
    public class PurchaseOrderListProjection : NpgsqlProjectionBase {
        public PurchaseOrderListProjection(string schema) : base(new Scripts(schema)) {
            When<CreateSchema>();

            When<PurchaseOrder>(po =>
                new[] {
                    Sql.UniqueIdentifier(Guid.Parse(po.PurchaseOrderId)).ToDbParameter("purchase_order_id"),
                    Sql.UniqueIdentifier(Guid.Parse(po.VendorId)).ToDbParameter("vendor_id"),
                    Sql.Int(po.PurchaseOrderNumber).ToDbParameter("purchase_order_number"),
                });
        }
    }
}
