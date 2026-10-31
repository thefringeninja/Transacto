namespace Transacto.Testing.Http;

public class HttpCentricTestSpecification {
	public HttpCentricTestSpecification(params HttpMessage[] messages) {
		ArgumentNullException.ThrowIfNull(messages);
		Messages = messages;
	}

	public HttpMessage[] Messages { get; }
}

public record HttpWait(TimeSpan Delay, CancellationToken CancellationToken = default);

public union HttpMessage(HttpRequest, HttpResponse, HttpWait);
