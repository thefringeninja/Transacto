namespace Transacto.Framework;

public readonly struct None;

public readonly record struct Some<T>(T Value) {
	public static implicit operator Some<T>(T value) => new(value);
}

public static class Option {
	public static Option<T> None<T>() => Option<T>.None;
	public static Option<T> Some<T>(T value) => new Some<T>(value);
}

public union Option<T>(None, Some<T>) {
	public static readonly Option<T> None = new None();
	public static Option<T> Some(T value) => new Some<T>(value);
	public bool IsNone => Value is None;
	public bool IsSome => Value is Some<T>;
}
