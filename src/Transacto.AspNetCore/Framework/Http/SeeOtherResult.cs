using System.Diagnostics.CodeAnalysis;

namespace Transacto.Framework.Http;

internal class SeeOtherResult([StringSyntax(StringSyntaxAttribute.Uri)] string location) : IResult {
	public Task ExecuteAsync(HttpContext httpContext) {
		httpContext.Response.StatusCode = 303;
		httpContext.Response.Headers.Location = location;
		return Task.CompletedTask;
	}
}
