using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;

namespace Transacto.Framework.Http; 

public sealed class TextResponse : Response {
	private static readonly MediaTypeHeaderValue ContentType = new("text/plain");
	private readonly string _body;

	public TextResponse(string body) {
		Headers.ContentType = ContentType;
		_body = body;
	}

	protected internal override ValueTask WriteBody(Stream stream, CancellationToken cancellationToken) => stream
		.WriteAsync(Encoding.UTF8.GetBytes(_body), cancellationToken);
}