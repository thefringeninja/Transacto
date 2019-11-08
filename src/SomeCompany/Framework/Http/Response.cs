using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SomeCompany.Framework.Http {
    public abstract class Response {
        public HttpStatusCode StatusCode { get; }
        public IDictionary<string, string[]> Headers { get; }

        protected Response(HttpStatusCode statusCode, MediaTypeHeaderValue mediaType = null)
        {
            StatusCode = statusCode;
            Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["content-type"] = new[] { mediaType?.ToString() }
            };
        }

        public abstract Task WriteBody(Stream stream, CancellationToken cancellationToken = default);
    }
}
