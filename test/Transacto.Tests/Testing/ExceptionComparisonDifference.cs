using System;

namespace Transacto.Testing;

/// <summary>
///     Represents a difference found between the expected and actual exception.
/// </summary>
public class ExceptionComparisonDifference {
	/// <summary>
	///     Initializes a new instance of the <see cref="ExceptionComparisonDifference" /> class.
	/// </summary>
	/// <param name="expected">The expected exception.</param>
	/// <param name="actual">The actual exception.</param>
	/// <param name="message">The message describing the difference.</param>
	public ExceptionComparisonDifference(Exception expected, Exception actual, string message) {
		Expected = expected;
		Actual = actual;
		Message = message;
	}

	/// <summary>
	///     Gets the expected exception.
	/// </summary>
	/// <value>
	///     The expected exception.
	/// </value>
	public Exception Expected { get; }

	/// <summary>
	///     Gets the actual exception.
	/// </summary>
	/// <value>
	///     The actual exception.
	/// </value>
	public Exception Actual { get; }

	/// <summary>
	///     Gets the message.
	/// </summary>
	/// <value>
	///     The message.
	/// </value>
	public string Message { get; }
}
