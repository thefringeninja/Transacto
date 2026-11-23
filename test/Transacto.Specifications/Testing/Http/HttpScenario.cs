namespace Transacto.Testing.Http;

/// <summary>
///     The when-then http test specification builder bootstrapper.
/// </summary>
public class HttpScenario : IHttpScenarioInitialStateBuilder {
	/// <summary>
	///     When one or more requests are sent.
	/// </summary>
	/// <param name="requests">The requests.</param>
	/// <returns>A builder continuation.</returns>
	public IHttpScenarioWhenStateBuilder When(params HttpRequest[] requests) {
		ArgumentNullException.ThrowIfNull(requests);
		return new HttpTestSpecificationBuilder().When(requests);
	}

	public IHttpScenarioThenStateBuilder Then(HttpRequest request, HttpResponse expected) {
		return new HttpTestSpecificationBuilder().Then(request, expected);
	}

	public IHttpScenarioThenStateBuilder ThenEventually(HttpRequest request, HttpResponse expected,
		TimeSpan? timeout = null) => new HttpTestSpecificationBuilder().ThenEventually(request, expected, timeout);
}
