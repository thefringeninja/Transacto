using System.Diagnostics.CodeAnalysis;
using NodaTime.Text;

// ReSharper disable once CheckNamespace
namespace NodaTime;

internal struct ParseableLocalDate(LocalDate value) : IParsable<ParseableLocalDate> {
	private readonly LocalDate _value = value;

	public static ParseableLocalDate Parse(string s, IFormatProvider? provider) =>
		new(LocalDatePattern.Iso.Parse(s).GetValueOrThrow());

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider,
		out ParseableLocalDate result) {
		if (s is null) {
			result = default;
			return false;
		}

		var parseResult = LocalDatePattern.Iso.Parse(s);
		if (!parseResult.Success) {
			result = default;
			return false;
		}

		result = new ParseableLocalDate(parseResult.Value);
		return true;
	}

	public static implicit operator LocalDate(ParseableLocalDate value) => value._value;
}
