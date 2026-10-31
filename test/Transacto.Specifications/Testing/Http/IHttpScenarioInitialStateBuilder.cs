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
}
