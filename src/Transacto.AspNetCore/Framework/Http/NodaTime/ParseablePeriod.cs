using System.Diagnostics.CodeAnalysis;
using NodaTime.Text;

namespace NodaTime;

internal struct ParseablePeriod(Period value) : IParsable<ParseablePeriod> {
	private readonly Period _value = value;

	public static ParseablePeriod Parse(string s, IFormatProvider? provider) =>
		new(PeriodPattern.Roundtrip.Parse(s).GetValueOrThrow());

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider,
		out ParseablePeriod result) {
		if (s is null) {
			result = default;
			return false;
		}

		var parseResult = PeriodPattern.Roundtrip.Parse(s);
		if (!parseResult.Success) {
			result = default;
			return false;
		}

		result = new ParseablePeriod(parseResult.Value);
		return true;
	}

	public static implicit operator Period(ParseablePeriod value) => value._value;
}
