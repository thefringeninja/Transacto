using Projac.Npgsql;
using SomeCompany.Framework.Projections;
using SomeCompany.Infrastructure;

namespace SomeCompany.PurchaseOrders {
    public class PurchaseOrderListProjection : NpgsqlProjectionBase {
        public PurchaseOrderListProjection() : base(new Scripts()) {
            When<CreateSchema>();

            When<PurchaseOrder>(po =>
                new[] {
                    Sql.GetParameter(() => po.PurchaseOrderId),
                    Sql.GetParameter(() => po.VendorId),
                    Sql.GetParameter(() => po.PurchaseOrderNumber)
                });
        }
    }
}
