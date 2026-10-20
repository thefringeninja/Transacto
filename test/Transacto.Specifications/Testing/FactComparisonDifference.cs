namespace Transacto.Testing;

/// <summary>
///     Represents a difference found between the expected and actual fact.
/// </summary>
public class FactComparisonDifference {
	/// <summary>
	///     Initializes a new instance of the <see cref="FactComparisonDifference" /> class.
	/// </summary>
	/// <param name="expected">The expected fact.</param>
	/// <param name="actual">The actual fact.</param>
	/// <param name="message">The message describing the difference.</param>
	public FactComparisonDifference(Fact expected, Fact actual, string message) {
		Expected = expected;
		Actual = actual;
		Message = message;
	}

	/// <summary>
	///     Gets the expected fact.
	/// </summary>
	/// <value>
	///     The expected fact.
	/// </value>
	public Fact Expected { get; }

	/// <summary>
	///     Gets the actual fact.
	/// </summary>
	/// <value>
	///     The actual fact.
	/// </value>
	public Fact Actual { get; }

	/// <summary>
	///     Gets the message.
	/// </summary>
	/// <value>
	///     The message.
	/// </value>
	public string Message { get; }
}
