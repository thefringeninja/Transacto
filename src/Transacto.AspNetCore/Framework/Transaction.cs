using KurrentDB.Client;

namespace Transacto.Framework;

/// <summary>
/// A <see cref="UnitOfWork"/> transaction.
/// </summary>
public record Transaction(string StreamName, IAggregateRoot Aggregate, StreamState Expected) {
	public static bool HasChanges(Transaction transaction) => transaction.Aggregate.HasChanges;
}
