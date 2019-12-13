using System;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SomeCompany.Framework.Http {
    public class JsonResponse : Response {
        private readonly dynamic _item;

        public JsonResponse(dynamic item) : base(HttpStatusCode.OK, new MediaTypeHeaderValue("application/json")) {
            _item = item;
        }

        public override Task WriteBody(Stream stream, CancellationToken cancellationToken = default) =>
            JsonSerializer.SerializeAsync(stream, _item, new JsonSerializerOptions(), cancellationToken);
    }
}
