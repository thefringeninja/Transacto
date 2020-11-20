using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;

namespace Transacto {
	public class Response {
		public virtual ResponseHeaders Headers { get; }
		public virtual HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

		public Response() {
			Headers = new ResponseHeaders(new HeaderDictionary());
		}

		public ValueTask Write(HttpResponse response) {
			response.StatusCode = (int)StatusCode;

			foreach (var (key, value) in Headers.Headers) {
				response.Headers.AppendCommaSeparatedValues(key, value);
			}

			return WriteBody(response.Body, response.HttpContext.RequestAborted);
		}

		protected internal virtual ValueTask WriteBody(Stream stream, CancellationToken cancellationToken) =>
			new(Task.CompletedTask);
	}
}
