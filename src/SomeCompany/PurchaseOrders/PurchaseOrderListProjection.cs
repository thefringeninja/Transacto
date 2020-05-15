using Projac.Npgsql;
using SomeCompany.Infrastructure;
using Transacto;

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
