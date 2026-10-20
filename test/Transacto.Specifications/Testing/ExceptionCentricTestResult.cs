using Transacto.Framework;

namespace Transacto.Testing;

/// <summary>
///     The result of an exception centric test specification.
/// </summary>
public class ExceptionCentricTestResult {
	private readonly TestResultState _state;

	/// <summary>
	///     Initializes a new instance of the <see cref="ExceptionCentricTestResult" /> class.
	/// </summary>
	/// <param name="specification">The specification.</param>
	/// <param name="state">The state.</param>
	/// <param name="actualException">The actual exception.</param>
	/// <param name="actualEvents">The actual events.</param>
	internal ExceptionCentricTestResult(ExceptionCentricTestSpecification specification, TestResultState state,
		Optional<Exception> actualException,
		Optional<Fact[]> actualEvents) {
		Specification = specification;
		_state = state;
		ButException = actualException;
		ButEvents = actualEvents;
	}

	/// <summary>
	///     Gets the test specification associated with this result.
	/// </summary>
	/// <value>
	///     The test specification.
	/// </value>
	public ExceptionCentricTestSpecification Specification { get; }

	/// <summary>
	///     Gets a value indicating whether this <see cref="EventCentricTestResult" /> has passed.
	/// </summary>
	/// <value>
	///     <c>true</c> if passed; otherwise, <c>false</c>.
	/// </value>
	public bool Passed => _state == TestResultState.Passed;

	/// <summary>
	///     Gets a value indicating whether this <see cref="EventCentricTestResult" /> has failed.
	/// </summary>
	/// <value>
	///     <c>true</c> if failed; otherwise, <c>false</c>.
	/// </value>
	public bool Failed => _state == TestResultState.Failed;

	/// <summary>
	///     Gets the exception that happened instead of the expected one, or empty if one didn't happen at all.
	/// </summary>
	/// <value>
	///     The exception.
	/// </value>
	public Optional<Exception> ButException { get; }

	/// <summary>
	///     Gets the events that happened instead of the expected exception, or empty if none happened at all.
	/// </summary>
	/// <value>
	///     The events.
	/// </value>
	public Optional<Fact[]> ButEvents { get; }
}
