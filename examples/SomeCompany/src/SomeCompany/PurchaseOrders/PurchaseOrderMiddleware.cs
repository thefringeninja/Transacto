using System.Net;
using Hallo;
using Microsoft.AspNetCore.Mvc;
using Transacto.Framework;
using Transacto.Framework.Http;

namespace SomeCompany.PurchaseOrders;

public static class PurchaseOrderMiddleware {
	public static void UsePurchaseOrders(this IEndpointRouteBuilder builder, PurchaseOrderRepository purchaseOrders) {
		builder.MapGet(string.Empty, async (CancellationToken ct) => {
			var orders = await purchaseOrders.List(ct);

			return Results.Extensions.Hal(orders, Checkpoint.None, PurchaseOrderListRepresentation.Instance);
		});

		builder.MapPost(string.Empty, async ([FromBody] PurchaseOrder purchaseOrder, CancellationToken ct) => {
			if (purchaseOrder.PurchaseOrderId == Guid.Empty) {
				purchaseOrder = purchaseOrder with {
					PurchaseOrderId = Guid.NewGuid()
				};
			}

			await purchaseOrders.Save(purchaseOrder, -1, ct);

			return Results.Extensions.Hal(purchaseOrder, 0.ToCheckpoint(),
				PurchaseOrderRepresentation.Instance, HttpStatusCode.Created);
		});

		builder.MapGet("{purchaseOrderId:guid}", async (Guid purchaseOrderId, CancellationToken ct) =>
			await purchaseOrders.Get(purchaseOrderId, ct) switch {
				{ BusinessTransaction.HasValue: true } envelope => Results.Extensions.Hal(
					envelope.BusinessTransaction.Value, envelope.ExpectedRevision.ToCheckpoint(),
					PurchaseOrderRepresentation.Instance),
				_ => Results.Extensions.Hal<PurchaseOrder>(null, Checkpoint.None,
					PurchaseOrderRepresentation.Instance, HttpStatusCode.NotFound)
			});

		builder.MapPut("{purchaseOrderId:guid}", async (Guid purchaseOrderId, DocumentRevision documentRevision,
			[FromBody] PurchaseOrder purchaseOrder, CancellationToken ct) => {
			purchaseOrder = purchaseOrder with {
				PurchaseOrderId = purchaseOrderId
			};

			await purchaseOrders.Save(purchaseOrder, documentRevision.Value.HasValue
				? documentRevision.Value.Value + 1
				: -1, ct);

			return Results.Extensions.Hal(purchaseOrder, Checkpoint.None, PurchaseOrderRepresentation.Instance);
		});

		builder.MapBusinessTransaction<PurchaseOrder>("{purchaseOrderId:guid}");
	}


	private class PurchaseOrderListRepresentation : Hal<IEnumerable<PurchaseOrder>>,
		IHalState<IEnumerable<PurchaseOrder>> {
		public static readonly PurchaseOrderListRepresentation Instance = new();
		public object StateFor(IEnumerable<PurchaseOrder> resource) => resource.ToArray();
	}

	private class PurchaseOrderRepresentation : Hal<PurchaseOrder>, IHalState<PurchaseOrder> {
		public object StateFor(PurchaseOrder resource) => resource;
		public static readonly PurchaseOrderRepresentation Instance = new();
	}
}
