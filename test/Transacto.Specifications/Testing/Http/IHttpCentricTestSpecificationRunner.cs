namespace Transacto.Testing.Http;

/// <summary>
///     Represents an http centric test specification runner.
/// </summary>
public interface IHttpCentricTestSpecificationRunner {
	/// <summary>
	///     Runs the specified test specification.
	/// </summary>
	/// <param name="specification">The test specification to run.</param>
	/// <returns>The result of running the test specification.</returns>
	Task<HttpCentricTestResult> Run(HttpCentricTestSpecification specification);
}
