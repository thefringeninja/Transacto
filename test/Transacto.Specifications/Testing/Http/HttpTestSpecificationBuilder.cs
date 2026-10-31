namespace Transacto.Testing.Http;

internal class HttpTestSpecificationBuilder : IHttpScenarioWhenStateBuilder, IHttpScenarioThenStateBuilder {
	private readonly HttpTestSpecificationBuilderContext _context;

	public HttpTestSpecificationBuilder() {
		_context = new HttpTestSpecificationBuilderContext();
	}

	private HttpTestSpecificationBuilder(HttpTestSpecificationBuilderContext context) {
		_context = context;
	}

	public IHttpScenarioWhenStateBuilder When(params HttpRequest[] requests) {
		ArgumentNullException.ThrowIfNull(requests);
		return new HttpTestSpecificationBuilder(
			_context.Append(Array.ConvertAll(requests, request => (HttpMessage)request)));
	}

	public IHttpScenarioThenStateBuilder Then(HttpRequest request, HttpResponse expected) {
		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(expected);
		return new HttpTestSpecificationBuilder(_context.Append(request).Append(expected));
	}

	public IHttpScenarioWhenStateBuilder Wait(TimeSpan delay, CancellationToken cancellationToken = default) {
		ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);
		return new HttpTestSpecificationBuilder(_context.Append(new HttpWait(delay, cancellationToken)));
	}

	public HttpCentricTestSpecification Build() => _context.ToHttpCentricSpecification();
}
