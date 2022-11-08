namespace Transacto.Testing;

/// <summary>
///     Represents the fact that an event happened to what is identified by the identifier.
/// </summary>
public struct Fact : IEquatable<Fact> {
	/// <summary>
	///     Returns an empty array of facts.
	/// </summary>
	public static readonly Fact[] Empty = new Fact[0];

	/// <summary>
	///     Initializes a new instance of the <see cref="Fact" /> struct.
	/// </summary>
	/// <param name="identifier">The identifier.</param>
	/// <param name="event">The event.</param>
	/// <exception cref="System.ArgumentNullException">
	///     Thrown if <paramref name="identifier" /> or <paramref name="event" /> is
	///     <c>null</c>.
	/// </exception>
	public Fact(string identifier, object @event) {
		if (identifier == null) throw new ArgumentNullException(nameof(identifier));
		if (@event == null) throw new ArgumentNullException(nameof(@event));
		Identifier = identifier;
		Event = @event;
	}

	/// <summary>
	///     Gets the identifier.
	/// </summary>
	/// <value>
	///     The identifier.
	/// </value>
	public string Identifier { get; }

	/// <summary>
	///     Gets the event.
	/// </summary>
	/// <value>
	///     The event.
	/// </value>
	public object Event { get; }

	/// <summary>
	///     Determines whether the specified <see cref="Fact" /> is equal to this instance.
	/// </summary>
	/// <param name="other">The <see cref="Fact" /> to compare with this instance.</param>
	/// <returns>
	///     <c>true</c> if the specified <see cref="Fact" /> is equal to this instance; otherwise, <c>false</c>.
	/// </returns>
	public bool Equals(Fact other) {
		return Identifier.Equals(other.Identifier) &&
		       Event.Equals(other.Event);
	}

	/// <summary>
	///     Determines whether the specified <see cref="System.Object" /> is equal to this instance.
	/// </summary>
	/// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
	/// <returns>
	///     <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
	/// </returns>
	public override bool Equals(object? obj) {
		if (ReferenceEquals(obj, null) || obj.GetType() != GetType())
			return false;

		return Equals((Fact)obj);
	}

	/// <summary>
	///     Returns a hash code for this instance.
	/// </summary>
	/// <returns>
	///     A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
	/// </returns>
	public override int GetHashCode() {
		return Identifier.GetHashCode() ^ Event.GetHashCode();
	}
}
