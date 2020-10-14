using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Transacto {
	public sealed class NotFoundResponse : Response {
		public NotFoundResponse() {
			StatusCode = HttpStatusCode.NotFound;
		}

		protected internal override ValueTask WriteBody(Stream stream, CancellationToken cancellationToken) =>
			new ValueTask(Task.CompletedTask);
	}
}
