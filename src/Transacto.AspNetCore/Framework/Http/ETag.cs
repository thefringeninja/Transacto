using System;
using EventStore.Client;
using HashCode = System.HashCode;

namespace Transacto.Framework.Http; 

public struct ETag : IEquatable<ETag> {
	public static readonly ETag None = default;
	private readonly string _value;

	public static ETag Create(Optional<Position> position) =>
		Create(position.HasValue ? position.Value : Position.Start);

	public static ETag Create(long? position) => position.HasValue
		? new ETag(position.Value.ToString())
		: None;

	private static ETag Create(Position position) => new($"{position.CommitPosition}/{position.PreparePosition}");

	private ETag(string value) {
		_value = value;
	}

	public bool Equals(ETag other) => _value == other._value;
	public override bool Equals(object? obj) => obj is ETag other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_value);
	public static bool operator ==(ETag left, ETag right) => left.Equals(right);
	public static bool operator !=(ETag left, ETag right) => !left.Equals(right);
	public override string ToString() => _value;
}