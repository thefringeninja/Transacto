using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Hallo;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Transacto;
using Transacto.Framework;

namespace SomeCompany.PurchaseOrders {
	public static class PurchaseOrderMiddleware {
		public static void UsePurchaseOrders(this IEndpointRouteBuilder builder,
			PurchaseOrderRepository purchaseOrders) {
			builder
				.MapGet(string.Empty, async context => {
					var orders = await purchaseOrders.List(context.RequestAborted);

					return new HalResponse(context.Request, PurchaseOrderListRepresentation.Instance,
						ETag.Create(orders.Max(x => x.Position)), new Optional<object>(orders));
				})
				.MapPost(string.Empty, async (HttpContext context, PurchaseOrder purchaseOrder) => {
					if (purchaseOrder.PurchaseOrderId == Guid.Empty) {
						purchaseOrder.PurchaseOrderId = Guid.NewGuid();
					}

					await purchaseOrders.Save(purchaseOrder, context.RequestAborted);

					return new HalResponse(context.Request, PurchaseOrderRepresentation.Instance,
						ETag.Create(purchaseOrder.Version), purchaseOrder) {
						StatusCode = HttpStatusCode.Created,
						Headers = {
							Location = new Uri(purchaseOrder.PurchaseOrderId.ToString())
						}
					};
				})
				.MapGet("{purchaseOrderId:guid}", async context => {
					if (!context.TryParseGuid(nameof(PurchaseOrder.PurchaseOrderId), out var purchaseOrderId)) {
						return new HalResponse(context.Request, PurchaseOrderRepresentation.Instance) {
							StatusCode = HttpStatusCode.NotFound
						};
					}

					var order = await purchaseOrders.Get(purchaseOrderId, context.RequestAborted);

					return new HalResponse(context.Request, PurchaseOrderRepresentation.Instance,
						ETag.Create(order.HasValue ? order.Value.Position : new long?()), order) {
						StatusCode = order.HasValue ? HttpStatusCode.OK : HttpStatusCode.NotFound
					};
				})
				.MapPut("{purchaseOrderId:guid}", async (HttpContext context, PurchaseOrder purchaseOrder) => {
					if (!context.TryParseGuid(nameof(purchaseOrder.PurchaseOrderId), out var purchaseOrderId)) {
						return new HalResponse(context.Request, PurchaseOrderRepresentation.Instance) {
							StatusCode = HttpStatusCode.NotFound
						};
					}

					purchaseOrder.PurchaseOrderId = purchaseOrderId;

					await purchaseOrders.Save(purchaseOrder, context.RequestAborted);

					return new HalResponse(context.Request, PurchaseOrderRepresentation.Instance,
						ETag.Create(purchaseOrder.Version), purchaseOrder);
				})
				.MapBusinessTransaction<PurchaseOrder>("{purchaseOrderId:guid}");
		}


		private class PurchaseOrderListRepresentation : Hal<IEnumerable<PurchaseOrder>>,
			IHalState<IEnumerable<PurchaseOrder>> {
			public static readonly PurchaseOrderListRepresentation Instance = new PurchaseOrderListRepresentation();
			public object StateFor(IEnumerable<PurchaseOrder> resource) => resource.ToArray();
		}

		private class PurchaseOrderRepresentation : Hal<PurchaseOrder>, IHalState<PurchaseOrder> {
			public object StateFor(PurchaseOrder resource) => resource;
			public static readonly PurchaseOrderRepresentation Instance = new PurchaseOrderRepresentation();
		}
	}
}
