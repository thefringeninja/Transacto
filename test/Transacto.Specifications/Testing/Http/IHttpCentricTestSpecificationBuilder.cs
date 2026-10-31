namespace Transacto.Testing.Http;

/// <summary>
///     The act of building an http-centric test specification.
/// </summary>
public interface IHttpCentricTestSpecificationBuilder {
	/// <summary>
	///     Builds the test specification thus far.
	/// </summary>
	/// <returns>The test specification.</returns>
	HttpCentricTestSpecification Build();
}
