namespace Transacto.Testing.Http;

/// <summary>
///     The initial state within the http test specification building process.
/// </summary>
public interface IHttpScenarioInitialStateBuilder {
	/// <summary>
	///     When one or more requests are sent.
	/// </summary>
	/// <param name="requests">The requests.</param>
	/// <returns>A builder continuation.</returns>
	IHttpScenarioWhenStateBuilder When(params HttpRequest[] requests);
	
	IHttpScenarioThenStateBuilder Then(HttpRequest request, HttpResponse expected);

	/// <summary>
	///     Then sending the request should eventually yield the expected response. The request is repeated until the
	///     response matches or the timeout passes, e.g. to let projections catch up.
	/// </summary>
	/// <param name="request">The request to send.</param>
	/// <param name="expected">The expected response.</param>
	/// <param name="timeout">How long to keep trying. Defaults to ten seconds.</param>
	/// <returns>A builder continuation.</returns>
	IHttpScenarioThenStateBuilder ThenEventually(HttpRequest request, HttpResponse expected, TimeSpan? timeout = null);
}
