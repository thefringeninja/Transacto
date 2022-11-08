using Microsoft.AspNetCore.Http;

namespace Transacto.Framework.Http;

internal class NotAcceptableResult : IResult {
	public static readonly NotAcceptableResult Instance = new();

	public Task ExecuteAsync(HttpContext context) {
		context.Response.StatusCode = 406;
		context.Response.ContentType = "text/plain";

		return context.Response.WriteAsync("No.");
	}
}
