using System.Collections.Immutable;
using Dapper;
using Npgsql;
using SqlStreamStore;
using Transacto.Framework.Http;
using Transacto.Infrastructure.SqlStreamStore;

namespace SomeCompany.PurchaseOrders;

public class PurchaseOrderRepository {
	private readonly string _schema;
	private readonly Func<CancellationToken, ValueTask<NpgsqlConnection>> _connectionFactory;
	private readonly StreamStoreBusinessTransactionRepository<PurchaseOrder> _inner;

	public PurchaseOrderRepository(IStreamStore streamStore, string schema,
		Func<CancellationToken, ValueTask<NpgsqlConnection>> connectionFactory) {
		_schema = schema;
		_connectionFactory = connectionFactory;
		_inner = new StreamStoreBusinessTransactionRepository<PurchaseOrder>(streamStore,
			order => GetStreamName(order.PurchaseOrderId),
			TransactoHttpSerializerOptions.BusinessTransactions(typeof(PurchaseOrder)));
	}

	private static string GetStreamName(Guid purchaseOrderId) => $"purchaseOrder-{purchaseOrderId:n}";

	public ValueTask<BusinessTransactionEnvelope<PurchaseOrder>> Get(Guid purchaseOrderId,
		CancellationToken cancellationToken = default) =>
		_inner.GetOptional(GetStreamName(purchaseOrderId), cancellationToken);

	public async Task<ImmutableArray<PurchaseOrderListItem>> List(int page, CancellationToken cancellationToken) {
		const int pageSize = 10;
		await using var connection = await _connectionFactory(cancellationToken);
		var items = await connection
			.QueryUnbufferedAsync<(Guid purchaseOrderId, Guid vendorId, int purchaseOrderNumber)>(
				"""
					SELECT p.purchase_order_id, p.vendor_id, p.purchase_order_number
					FROM purchase_order_list p
					ORDER BY p.purchase_order_number
					LIMIT @pageSize
					OFFSET (@page - 1) * @pageSize 
				""", new { pageSize, page })
			.Select(row => new PurchaseOrderListItem(row.purchaseOrderId, row.vendorId, row.purchaseOrderNumber))
			.ToArrayAsync(cancellationToken);
		return [..items];
	}

	public ValueTask Save(PurchaseOrder purchaseOrder, int expectedRevision,
		CancellationToken cancellationToken = default) =>
		_inner.Save(purchaseOrder, expectedRevision, cancellationToken);
}
