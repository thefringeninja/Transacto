namespace SomeCompany.PurchaseOrders;

public record PurchaseOrderListItem(Guid PurchaseOrderId, Guid VendorId, int PurchaseOrderNumber);
