namespace Transacto.Testing.Http;

public class HttpCentricTestSpecification {
	public HttpCentricTestSpecification(params HttpMessage[] messages) {
		ArgumentNullException.ThrowIfNull(messages);
		Messages = messages;
	}

	public HttpMessage[] Messages { get; }
}

public union HttpMessage(HttpRequest, HttpResponse);
