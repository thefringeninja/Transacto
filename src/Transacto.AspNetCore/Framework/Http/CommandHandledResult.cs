namespace Transacto.Framework.Http;

internal class CommandHandledResult(Checkpoint checkpoint) : IResult {
	public Task ExecuteAsync(HttpContext context) => Results.Text(checkpoint.ToString()).ExecuteAsync(context);
}
