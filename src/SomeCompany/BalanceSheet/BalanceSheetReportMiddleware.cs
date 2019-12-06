using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SomeCompany.BalanceSheet;
using MidFunc = System.Func<Microsoft.AspNetCore.Http.HttpContext, System.Func<System.Threading.Tasks.Task>,
    System.Threading.Tasks.Task>;

// ReSharper disable CheckNamespace
namespace Microsoft.AspNetCore.Builder {
// ReSharper restore CheckNamespace

    public static class BalanceSheetReportMiddleware {
        public static IApplicationBuilder UseBalanceSheet(this IApplicationBuilder builder,
            BalanceSheetReportResource resource) {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (resource == null) throw new ArgumentNullException(nameof(resource));
            return builder.UseRouter(router => router.MapMiddlewareGet("{thru}", inner => inner.Use(Get(resource))));
        }

        private static MidFunc Get(BalanceSheetReportResource resource) => async (context, next) => {
            var response =
                await resource.Get(Convert.ToDateTime(context.GetRouteValue("thru")), context.RequestAborted);

            await context.WriteResponse(response);
        };
    }
}
