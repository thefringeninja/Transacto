using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Transacto {
	public class NotFoundResponse : Response {
		public NotFoundResponse() {
			StatusCode = HttpStatusCode.NotFound;
		}

		protected override ValueTask WriteBody(Stream stream, CancellationToken cancellationToken = default) =>
			new ValueTask(Task.CompletedTask);
	}
}
