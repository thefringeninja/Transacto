using System.Net;
using Hallo;

namespace Transacto.Framework.Http;

public static class ResultExtensions {
	extension(Results) {
		public static IResult Hal<T>(T? resource, Checkpoint checkpoint, IHal hal,
			HttpStatusCode statusCode = HttpStatusCode.OK) =>
			new HalResult<T>(resource, checkpoint, hal, statusCode);

		public static IResult CommandHandled(Checkpoint checkpoint) => new CommandHandledResult(checkpoint);
	}
}
