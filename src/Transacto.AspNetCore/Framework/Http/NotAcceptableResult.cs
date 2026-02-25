namespace Transacto.Framework.Http;

internal class NotAcceptableResult : IResult {
	public static readonly NotAcceptableResult Instance = new();

	public Task ExecuteAsync(HttpContext context) {
		context.Response.StatusCode = 406;
		foreach (var accept in context.Request.GetTypedHeaders().Accept.OrderByDescending(h => h.Quality)) {
			if (accept.SubType != "json" && accept.Suffix != "json") continue;
			return Results.Problem(statusCode: 406).ExecuteAsync(context);
		}
		context.Response.ContentType = "text/plain";

		return context.Response.WriteAsync("Not acceptable.");
	}
}
