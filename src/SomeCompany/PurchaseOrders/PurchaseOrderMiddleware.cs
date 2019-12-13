using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SomeCompany.Framework.Http;
using SqlStreamStore;
using MidFunc = System.Func<Microsoft.AspNetCore.Http.HttpContext, System.Func<System.Threading.Tasks.Task>,
    System.Threading.Tasks.Task>;

namespace SomeCompany.PurchaseOrders {
    public static class PurchaseOrderMiddleware {
        public static IApplicationBuilder UsePurchaseOrders(this IApplicationBuilder builder,
            PurchaseOrderResource purchaseOrders,
            PurchaseOrderListResource purchaseOrderList) {
            return builder.UseRouter(
                router => router
                    .MapMiddlewareGet("list", inner => inner.Use(Get(purchaseOrderList)))
                    .MapMiddlewareGet("{purchaseOrderId:guid}", inner => inner.Use(Get(purchaseOrders)))
            );
        }

        private static MidFunc Get(PurchaseOrderResource purchaseOrders) => async (context, next) => {
            var response = await purchaseOrders.Get(Guid.Parse(context.GetRouteValue("purchaseOrderId").ToString()),
                context.RequestAborted);

            await context.WriteResponse(response);
        };

        private static MidFunc Get(PurchaseOrderListResource purchaseOrderList) => async (context, next) => {
            var response = await purchaseOrderList.Get(context.RequestAborted);
            await context.WriteResponse(response);
        };
    }

    public class PurchaseOrderResource {
        private readonly IStreamStore _streamStore;

        public PurchaseOrderResource(IStreamStore streamStore) {
            _streamStore = streamStore;
        }

        public async Task<Response> Get(Guid purchaseOrderId, CancellationToken cancellationToken) {
            var purchaseOrder = await _streamStore.ReadStreamBackwards($"purchaseOrder-{purchaseOrderId:n}",
                int.MaxValue, 1, true, cancellationToken);

            return new JsonResponse(purchaseOrder);
        }
    }

    public class PurchaseOrderListResource {
        private readonly Func<IDbConnection> _connectionFactory;
        private readonly string _schema;

        public PurchaseOrderListResource(Func<IDbConnection> connectionFactory, string schema) {
            _connectionFactory = connectionFactory;
            _schema = schema;
        }

        public async Task<Response> Get(CancellationToken cancellationToken) {
            using var connection = _connectionFactory();

            var results = await connection.QueryAsync<PurchaseOrder>($"SELECT * FROM {_schema}.purchase_orders");

            return new JsonResponse(results);
        }
    }
}
