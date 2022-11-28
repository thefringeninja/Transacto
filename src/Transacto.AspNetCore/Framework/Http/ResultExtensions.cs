using System.Net;
using Hallo;

namespace Transacto.Framework.Http;

public static class ResultExtensions {
	public static IResult Hal<T>(this IResultExtensions _, T? resource, Checkpoint checkpoint, IHal hal, HttpStatusCode statusCode = HttpStatusCode.OK) =>
		new HalResult<T>(resource, checkpoint, hal, statusCode);

	public static IResult NotAcceptable(this IResultExtensions _) => NotAcceptableResult.Instance;

	public static IResult PreconditionFailed(this IResultExtensions _) => new PreconditionFailedResult();

	public static IResult CommandHandled(this IResultExtensions _, Checkpoint checkpoint) =>
		new CommandHandledResult(checkpoint);
}
