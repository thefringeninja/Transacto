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

		protected internal override ValueTask WriteBody(Stream stream, CancellationToken cancellationToken) =>
			stream.WriteAsync(_checkpoint.Memory, cancellationToken);
	}
}
