namespace Transacto.Testing.Http;

/// <summary>
///     The then state within the http test specification building process.
/// </summary>
public interface IHttpScenarioThenStateBuilder : IHttpCentricTestSpecificationBuilder {
	/// <summary>
	///     When another request is sent.
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
	///     Waits before continuing, e.g. to let projections catch up.
	/// </summary>
	/// <param name="delay">How long to wait.</param>
	/// <param name="cancellationToken">A token that cancels the wait.</param>
	/// <returns>A builder continuation.</returns>
	IHttpScenarioWhenStateBuilder Wait(TimeSpan delay, CancellationToken cancellationToken = default);
}
