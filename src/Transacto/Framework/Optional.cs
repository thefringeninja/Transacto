namespace Transacto.Framework;

public readonly struct Optional<T>(T value) : IEquatable<Optional<T>> {
	public static readonly Optional<T> Empty = default;
	public bool HasValue { get; } = true;

	public T Value => HasValue ? _value : throw new InvalidOperationException();

	private readonly T _value = value;

	public bool Equals(Optional<T> other) =>
		HasValue == other.HasValue && EqualityComparer<T>.Default.Equals(_value, other._value);

	public override bool Equals(object? obj) => obj is Optional<T> other && Equals(other);
	public static bool operator ==(Optional<T> left, Optional<T> right) => left.Equals(right);
	public static bool operator !=(Optional<T> left, Optional<T> right) => !left.Equals(right);
	public static implicit operator Optional<T>(T value) => new(value);

	public TResult? ToNullable<TResult>() where TResult : unmanaged, T =>
		HasValue ? new TResult?((TResult)Value!) : null;

	public TResult? ToObject<TResult>() where TResult : class, T =>
		HasValue ? (TResult)Value! : null;
	
	public override int GetHashCode() => HasValue ? Value?.GetHashCode() ?? 0 : 0;
}
