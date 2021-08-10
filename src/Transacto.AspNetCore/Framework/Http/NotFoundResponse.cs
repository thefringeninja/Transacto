using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Transacto.Framework.Http {
	public sealed class NotFoundResponse : Response {
		public NotFoundResponse() {
			StatusCode = HttpStatusCode.NotFound;
		}

		protected internal override ValueTask WriteBody(Stream stream, CancellationToken cancellationToken) =>
			new(Task.CompletedTask);
	}
}
