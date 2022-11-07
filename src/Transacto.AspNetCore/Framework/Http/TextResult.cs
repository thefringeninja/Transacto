using System.Net;
using Microsoft.AspNetCore.Http;

namespace Transacto.Framework.Http;

internal class TextResult : IResult {
	private readonly string _text;
	private readonly HttpStatusCode _statusCode;

	public TextResult(string text, HttpStatusCode statusCode) {
		_text = text;
		_statusCode = statusCode;
	}
	public Task ExecuteAsync(HttpContext context) {
		context.Response.StatusCode = (int)_statusCode;
		context.Response.ContentType = "text/plain";

		return context.Response.WriteAsync(_text, context.RequestAborted);
	}
}
