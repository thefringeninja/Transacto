using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using SqlStreamStore;
using Transacto.Framework;
using Transacto.Infrastructure;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace SomeCompany.PurchaseOrders {
	public class PurchaseOrderRepository {
		private readonly string _schema;
		private readonly Func<CancellationToken, Task<NpgsqlConnection>> _connectionFactory;
		private readonly StreamStoreBusinessTransactionRepository<PurchaseOrder> _inner;

		public PurchaseOrderRepository(IStreamStore streamStore, string schema,
			Func<CancellationToken, Task<NpgsqlConnection>> connectionFactory) {
			_schema = schema;
			_connectionFactory = connectionFactory;
			_inner = new StreamStoreBusinessTransactionRepository<PurchaseOrder>(streamStore,
				order => GetStreamName(order.PurchaseOrderId), new JsonSerializerOptions());
		}

		private static string GetStreamName(Guid purchaseOrderId) => $"purchaseorder-{purchaseOrderId:n}";

		public ValueTask<Optional<PurchaseOrder>> Get(Guid purchaseOrderId,
			CancellationToken cancellationToken = default) =>
			_inner.GetOptional(GetStreamName(purchaseOrderId), cancellationToken);

		public async ValueTask<IEnumerable<PurchaseOrder>> List(CancellationToken cancellationToken) {
			await using var connection = await _connectionFactory(cancellationToken);
			return await connection.QueryAsync<PurchaseOrder>($"SELECT * FROM {_schema}.purchase_orders");
		}

		public ValueTask Save(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default) =>
			_inner.Save(purchaseOrder, cancellationToken);
	}
}
