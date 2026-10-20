namespace Transacto.Testing;

/// <summary>
///     Represents a difference found between the expected and actual result.
/// </summary>
public class ResultComparisonDifference {
	/// <summary>
	///     Initializes a new instance of the <see cref="ResultComparisonDifference" /> class.
	/// </summary>
	/// <param name="expected">The expected result.</param>
	/// <param name="actual">The actual result.</param>
	/// <param name="message">The message describing the difference.</param>
	public ResultComparisonDifference(object expected, object actual, string message) {
		Expected = expected;
		Actual = actual;
		Message = message;
	}

	/// <summary>
	///     Gets the expected result.
	/// </summary>
	/// <value>
	///     The expected result.
	/// </value>
	public object Expected { get; }

	/// <summary>
	///     Gets the actual result.
	/// </summary>
	/// <value>
	///     The actual result.
	/// </value>
	public object Actual { get; }

	/// <summary>
	///     Gets the message.
	/// </summary>
	/// <value>
	///     The message.
	/// </value>
	public string Message { get; }
}
