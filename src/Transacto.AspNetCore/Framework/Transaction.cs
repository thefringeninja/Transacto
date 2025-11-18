using Transacto.Infrastructure.EventStore;

namespace Transacto.Framework;

/// <summary>
/// A <see cref="UnitOfWork"/> transaction.
/// </summary>
public record Transaction(string StreamName, IAggregateRoot Aggregate, Expected Expected) {
	public static bool HasChanges(Transaction transaction) => transaction.Aggregate.HasChanges;
}
