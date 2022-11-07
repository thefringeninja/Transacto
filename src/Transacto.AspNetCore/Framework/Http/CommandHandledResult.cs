using Microsoft.AspNetCore.Http;

namespace Transacto.Framework.Http;

internal class CommandHandledResult : IResult {
	private readonly Checkpoint _checkpoint;

	public CommandHandledResult(Checkpoint checkpoint) {
		_checkpoint = checkpoint;
	}

	public Task ExecuteAsync(HttpContext context) =>
		context.Response.WriteAsync(_checkpoint.ToString());
}
