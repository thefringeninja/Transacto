using System.Collections.ObjectModel;

namespace Transacto.Framework;

/// <summary>
/// Tracks changes of attached aggregates.
/// </summary>
public class UnitOfWork {
	private static readonly AsyncLocal<UnitOfWork?> Storage = new();

	private readonly KeyedCollection<string, Transaction> _transactions;

	/// <summary>
	/// Starts a <see cref="UnitOfWork"/>.
	/// </summary>
	/// <returns>An <see cref="IDisposable"/> that ends the <see cref="UnitOfWork"/> when invoked.</returns>
	public static IDisposable Start() {
		Storage.Value = new UnitOfWork();

		return new DisposableAction(() => Storage.Value = null);
	}

	/// <summary>
	/// The current <see cref="UnitOfWork"/>.
	/// </summary>
	public static UnitOfWork Current => Storage.Value ?? throw new UnitOfWorkNotStartedException();

	/// <summary>
	/// Initializes a new instance of the <see cref="UnitOfWork"/> class.
	/// </summary>
	private UnitOfWork() {
		_transactions = new TransactionCollection();
	}

	/// <summary>
	/// Attaches the specified aggregate.
	/// </summary>
	/// <param name="transaction">The <see cref="Transaction"/>.</param>
	/// <exception cref="ArgumentException">Thrown when <see cref="aggregate"/> has already been attached.</exception>
	public void Attach(Transaction transaction) {
		if (_transactions.Contains(transaction.StreamName))
			throw new ArgumentException();
		_transactions.Add(transaction);
	}

	/// <summary>
	/// Attempts to get the <see cref="AggregateRoot"/> using the specified aggregate identifier.
	/// </summary>
	/// <param name="streamName">The aggregate identifier.</param>
	/// <param name="aggregate">The aggregate if found, otherwise <c>null</c>.</param>
	/// <returns><c>true</c> if the aggregate was found, otherwise <c>false</c>.</returns>
	public bool TryGet(string streamName, out IAggregateRoot? aggregate) {
		aggregate = null;
		if (!_transactions.TryGetValue(streamName, out var transaction)) {
			return false;
		}

		aggregate = transaction.Aggregate;
		return true;
	}

	/// <summary>
	/// Determines whether this instance has aggregates with state changes.
	/// </summary>
	/// <returns>
	///   <c>true</c> if this instance has aggregates with state changes; otherwise, <c>false</c>.
	/// </returns>
	public bool HasChanges => _transactions.Any(_ => _.Aggregate.HasChanges);

	/// <summary>
	/// Gets the aggregates with state changes.
	/// </summary>
	/// <returns>An enumeration of <see cref="Transaction"/>.</returns>
	public IEnumerable<Transaction> GetChanges() => _transactions.Where(_ => _.Aggregate.HasChanges);

	private class TransactionCollection : KeyedCollection<string, Transaction> {
		protected override string GetKeyForItem(Transaction item) => item.StreamName;
	}

	private class DisposableAction : IDisposable {
		private readonly Action _onDispose;

		public DisposableAction(Action onDispose) {
			_onDispose = onDispose;
		}

		public void Dispose() => _onDispose();
	}
}
