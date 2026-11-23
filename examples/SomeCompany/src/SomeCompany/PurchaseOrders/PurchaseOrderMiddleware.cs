using System.Collections.Immutable;
using System.Net;
using Hallo;
using Microsoft.AspNetCore.Mvc;
using Transacto.Framework;
using Transacto.Framework.Http;

namespace SomeCompany.PurchaseOrders;

public static class PurchaseOrderMiddleware {
	public static void UsePurchaseOrders(this IEndpointRouteBuilder builder) {
		builder.MapGet(string.Empty, async (
			[FromServices] PurchaseOrderRepository purchaseOrders,
			[FromQuery] int? page,
			CancellationToken ct = default) => {
			if (page is null or < 1) {
				return Results.Redirect("?page=1", true, true);
			}

			var items = await purchaseOrders.List(page.Value, ct);

			return Results.Hal(new PurchaseOrderListPage(items), Checkpoint.None,
				PurchaseOrderListRepresentation.Instance);
		}).CacheOutput();

		builder.MapPost(string.Empty, async (
			[FromBody] PurchaseOrder purchaseOrder,
			[FromServices] PurchaseOrderRepository purchaseOrders,
			CancellationToken ct) => {
			if (purchaseOrder.PurchaseOrderId == Guid.Empty) {
				purchaseOrder = purchaseOrder with {
					PurchaseOrderId = Guid.NewGuid()
				};
			}

			await purchaseOrders.Save(purchaseOrder, -1, ct);

			return Results.Hal(purchaseOrder, 0.ToCheckpoint(), PurchaseOrderRepresentation.Instance,
				HttpStatusCode.Created);
		});

		builder.MapGet("{purchaseOrderId:guid}", async (
				Guid purchaseOrderId,
				[FromServices] PurchaseOrderRepository purchaseOrders,
				CancellationToken ct) =>
			await purchaseOrders.Get(purchaseOrderId, ct) switch {
				{ BusinessTransaction: Some<PurchaseOrder> { Value: var value } } envelope => Results.Hal(
					value, envelope.ExpectedRevision.ToCheckpoint(),
					PurchaseOrderRepresentation.Instance),
				_ => Results.Hal<PurchaseOrder>(null, Checkpoint.None,
					PurchaseOrderRepresentation.Instance, HttpStatusCode.NotFound)
			}).CacheOutput();

		builder.MapPut("{purchaseOrderId:guid}", async (
			Guid purchaseOrderId,
			DocumentRevision documentRevision,
			[FromBody] PurchaseOrder purchaseOrder,
			[FromServices] PurchaseOrderRepository purchaseOrders,
			CancellationToken ct) => {
			purchaseOrder = purchaseOrder with {
				PurchaseOrderId = purchaseOrderId
			};
			
			var expectedRevision = documentRevision.Value switch{
				Some<int> { Value: var value } => value + 1,
				None => -1
			};

			await purchaseOrders.Save(purchaseOrder, expectedRevision, ct);

			return Results.Hal(purchaseOrder, Checkpoint.None, PurchaseOrderRepresentation.Instance);
		});

		builder.MapBusinessTransaction<PurchaseOrder>("{purchaseOrderId:guid}");
	}
}

public record PurchaseOrderListPage(ImmutableArray<PurchaseOrderListItem> Items);

public class PurchaseOrderListRepresentation : Hal<PurchaseOrderListPage>,
	IHalState<PurchaseOrderListPage> {
	public static readonly PurchaseOrderListRepresentation Instance = new();
	public object StateFor(PurchaseOrderListPage resource) => resource;
}

public class PurchaseOrderRepresentation : Hal<PurchaseOrder>, IHalState<PurchaseOrder> {
	public object StateFor(PurchaseOrder resource) => resource;
	public static readonly PurchaseOrderRepresentation Instance = new();
}
