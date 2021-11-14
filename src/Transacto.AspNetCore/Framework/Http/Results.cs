using System.Net;
using Hallo;
using Microsoft.AspNetCore.Http;

namespace Transacto.Framework.Http;

public static class Results {
	public static IResult Hal<T>(T resource, Checkpoint checkpoint, IHal hal) =>
		new HalResult<T>(resource, checkpoint, hal);

	public static IResult NotAcceptable() => NotAcceptableResult.Instance;

	public static IResult PreconditionFailed() => new PreconditionFailedResult();

	public static IResult Text(string text, HttpStatusCode statusCode = HttpStatusCode.OK)
		=> new TextResult(text, statusCode);

	public static IResult CommandHandled(Checkpoint checkpoint) => new CommandHandledResult(checkpoint);
}
