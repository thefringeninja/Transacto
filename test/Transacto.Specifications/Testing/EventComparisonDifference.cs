namespace Transacto.Testing;

/// <summary>
///     Represents a difference found between the expected and actual event.
/// </summary>
public class EventComparisonDifference {
	/// <summary>
	///     Initializes a new instance of the <see cref="EventComparisonDifference" /> class.
	/// </summary>
	/// <param name="expected">The expected event.</param>
	/// <param name="actual">The actual event.</param>
	/// <param name="message">The message describing the difference.</param>
	public EventComparisonDifference(object expected, object actual, string message) {
		Expected = expected;
		Actual = actual;
		Message = message;
	}

	/// <summary>
	///     Gets the expected event.
	/// </summary>
	/// <value>
	///     The expected event.
	/// </value>
	public object Expected { get; }

	/// <summary>
	///     Gets the actual event.
	/// </summary>
	/// <value>
	///     The actual event.
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
