using System;
using System.Threading.Tasks;
using SomeCompany.Framework.Http;

namespace Microsoft.AspNetCore.Http {
    internal static class HttpContextExtensions {
        public static Task WriteResponse(this HttpContext context, Response response) {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (response == null) throw new ArgumentNullException(nameof(response));

            context.Response.StatusCode = (int)response.StatusCode;

            foreach(var (key, value) in response.Headers)
            {
                context.Response.Headers.AppendCommaSeparatedValues(key, value);
            }

            return response.WriteBody(context.Response.Body, context.RequestAborted);
        }
    }
}
