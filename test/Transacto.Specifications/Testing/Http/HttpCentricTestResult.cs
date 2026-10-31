namespace Transacto.Testing.Http;

/// <summary>
///     The result of an http centric test specification.
/// </summary>
public class HttpCentricTestResult {
	private readonly TestResultState _state;

	internal HttpCentricTestResult(HttpCentricTestSpecification specification, TestResultState state,
		int step = -1, HttpRequest? request = null, HttpResponse? expected = null, HttpResponse? actual = null,
		Exception? exception = null) {
		Specification = specification;
		_state = state;
		Step = step;
		Request = request;
		Expected = expected;
		Actual = actual;
		Exception = exception;
	}

	public HttpCentricTestSpecification Specification { get; }
	public bool Passed => _state == TestResultState.Passed;
	public bool Failed => _state == TestResultState.Failed;

	/// <summary>The index of the failing message, or -1.</summary>
	public int Step { get; }

	/// <summary>The request that produced <see cref="Actual" />, if any.</summary>
	public HttpRequest? Request { get; }

	public HttpResponse? Expected { get; }
	public HttpResponse? Actual { get; }
	public Exception? Exception { get; }
}
