namespace Transacto.Framework {
	/// <summary>
	/// A <see cref="UnitOfWork"/> transaction.
	/// </summary>
	public record Transaction(string StreamName, AggregateRoot Aggregate, Optional<long> ExpectedVersion = default);
}
