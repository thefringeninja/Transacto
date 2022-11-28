using Projac.Npgsql;
using Transacto.Framework.Projections;
using Transacto.Infrastructure.Npgsql;

namespace SomeCompany.PurchaseOrders;

public class PurchaseOrderListProjection : NpgsqlProjection {
	public PurchaseOrderListProjection() : base(new Scripts()) {
		When<CreateSchema>();

		When<PurchaseOrder>(po =>
			new[] {
				Sql.Parameter(() => po.PurchaseOrderId),
				Sql.Parameter(() => po.VendorId),
				Sql.Parameter(() => po.PurchaseOrderNumber)
			});
	}
}
