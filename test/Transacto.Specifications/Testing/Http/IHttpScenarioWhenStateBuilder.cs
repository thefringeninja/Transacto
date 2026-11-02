namespace Transacto.Testing.Http;

/// <summary>
///     The when state within the http test specification building process.
/// </summary>
public interface IHttpScenarioWhenStateBuilder : IHttpCentricTestSpecificationBuilder {
	/// <summary>
	///     When another request is sent. The response to the previous request is not asserted.
	/// </summary>
	/// <param name="requests">The requests.</param>
	/// <returns>A builder continuation.</returns>
	IHttpScenarioWhenStateBuilder When(params HttpRequest[] requests);

	/// <summary>
	///     Then sending the request should yield the expected response.
	/// </summary>
	/// <param name="request">The request to send.</param>
	/// <param name="expected">The expected response.</param>
	/// <returns>A builder continuation.</returns>
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

	/// <summary>
	///     Waits before continuing, e.g. to let projections catch up.
	/// </summary>
	/// <param name="delay">How long to wait.</param>
	/// <param name="cancellationToken">A token that cancels the wait.</param>
	/// <returns>A builder continuation.</returns>
	IHttpScenarioWhenStateBuilder Wait(TimeSpan delay, CancellationToken cancellationToken = default);
}
