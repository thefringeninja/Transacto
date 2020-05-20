using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;

namespace Transacto {
	public sealed class CommandHandledResponse : Response {
		private readonly Position _position;

		public CommandHandledResponse(Position position) {
			_position = position;
		}

		protected internal override ValueTask WriteBody(Stream stream, CancellationToken cancellationToken) =>
			stream.WriteAsync(Encoding.UTF8.GetBytes($"{_position.CommitPosition}/{_position.PreparePosition}"),
				cancellationToken);
	}
}
