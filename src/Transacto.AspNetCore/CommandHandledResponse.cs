using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Transacto.Framework;

namespace Transacto {
	public sealed class CommandHandledResponse : Response {
		private readonly Checkpoint _checkpoint;

		public CommandHandledResponse(Checkpoint checkpoint) {
			_checkpoint = checkpoint;
		}

		protected internal override async ValueTask WriteBody(Stream stream, CancellationToken cancellationToken) {
			await using var writer = new StreamWriter(stream, leaveOpen: true);
			await writer.WriteAsync(_checkpoint.ToString());
		}
	}
}
