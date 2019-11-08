using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SqlStreamStore;
using Transacto.Infrastructure;

namespace SomeCompany.PurchaseOrders {
    public class PurchaseOrderRepository {
        private readonly SqlStreamStoreBusinessTransactionRepository<PurchaseOrder> _inner;

        public PurchaseOrderRepository(IStreamStore streamStore) {
            if (streamStore == null) throw new ArgumentNullException(nameof(streamStore));
            _inner = new SqlStreamStoreBusinessTransactionRepository<PurchaseOrder>(streamStore,
                order => GetStreamName(Guid.Parse((string) order.PurchaseOrderId)), new JsonSerializerOptions());
        }

        private static string GetStreamName(Guid purchaseOrderId) => $"purchaseorder-{purchaseOrderId:n}";

        public async ValueTask<PurchaseOrder> Get(Guid purchaseOrderId, CancellationToken cancellationToken = default) {
            var optionalPurchaseOrder = await _inner.GetOptional(GetStreamName(purchaseOrderId), cancellationToken);

            return optionalPurchaseOrder.HasValue
                ? optionalPurchaseOrder.Value
                : new PurchaseOrder {
                    PurchaseOrderId = purchaseOrderId.ToString()
                };
        }

        public ValueTask Save(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default) =>
            _inner.Save(purchaseOrder, cancellationToken);
    }
}
