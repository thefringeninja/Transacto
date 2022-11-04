using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Transacto.Framework.Http;

internal class PreconditionFailedResult : IResult {
	public Task ExecuteAsync(HttpContext context) {
		context.Response.StatusCode = 412;
		return Task.CompletedTask;
	}
}
