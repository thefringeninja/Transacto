using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

#nullable enable
namespace Transacto {
	public class Response {
		public IList<(string, StringValues)> Headers { get; }
		public HttpStatusCode StatusCode { get; set; }

		public Response() {
			Headers = new List<(string, StringValues)>();
			StatusCode = HttpStatusCode.OK;
		}

		public ValueTask Write(HttpResponse response) {
			response.StatusCode = (int)StatusCode;

			foreach (var (key, value) in Headers) {
				response.Headers.AppendCommaSeparatedValues(key, value);
			}

			return WriteBody(response.Body, response.HttpContext.RequestAborted);
		}

		protected virtual ValueTask WriteBody(Stream stream, CancellationToken cancellationToken = default) =>
			new ValueTask(Task.CompletedTask);
	}
}
