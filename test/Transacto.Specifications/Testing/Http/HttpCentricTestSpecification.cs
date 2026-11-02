namespace Transacto.Testing.Http;

public class HttpCentricTestSpecification {
	public HttpCentricTestSpecification(params HttpMessage[] messages) {
		ArgumentNullException.ThrowIfNull(messages);
		Messages = messages;
	}

	public HttpMessage[] Messages { get; }
}

public record HttpWait(TimeSpan Delay, CancellationToken CancellationToken = default);

/// <summary>
///     A request whose response is expected to match eventually, e.g. once a projection has caught up. The request
///     is re-sent on every attempt, so it should be safe to repeat (typically a GET).
/// </summary>
public record HttpEventually(HttpRequest Request, HttpResponse Expected, TimeSpan Timeout);

public union HttpMessage(HttpRequest, HttpResponse, HttpWait, HttpEventually);
