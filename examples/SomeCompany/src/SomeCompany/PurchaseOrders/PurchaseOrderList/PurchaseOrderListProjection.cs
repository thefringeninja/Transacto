using Inflector;
using Transacto.Infrastructure.Npgsql;

namespace SomeCompany.PurchaseOrders.PurchaseOrderList;

public class PurchaseOrderListProjection : NpgsqlProjection {
	public PurchaseOrderListProjection() : base(new Scripts()) {
		When<PurchaseOrder>(po => [
			Sql.UniqueIdentifier(po.PurchaseOrderId).ToDbParameter(nameof(PurchaseOrder.PurchaseOrderId).Underscore()),
			Sql.UniqueIdentifier(po.VendorId).ToDbParameter(nameof(PurchaseOrder.VendorId).Underscore()),
			Sql.Int(po.PurchaseOrderNumber).ToDbParameter(nameof(PurchaseOrder.PurchaseOrderNumber).Underscore()),
		]);
	}
}
