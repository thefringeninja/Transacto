using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Transacto.Messages;

namespace Transacto.Domain;

public record CashFlowCategory : IParsable<CashFlowCategory> {
	private readonly string? _value;
	private CashFlowCategory(string? value = null) => _value = value;

	public static CashFlowCategory Operations { get; } = new(nameof(Operations));
	public static CashFlowCategory Investment { get; } = new(nameof(Investment));
	public static CashFlowCategory Finance { get; } = new(nameof(Finance));

	public static CashFlowCategory None { get; } = new();

	public static ImmutableArray<CashFlowCategory> All { get; } =
		[new(nameof(Operations)), new(nameof(Investment)), new(nameof(Finance))];

	public Messages.CashFlowCategoryType ToEnum() => _value switch {
		nameof(Investment) => Messages.CashFlowCategoryType.Investment,
		nameof(Finance) => Messages.CashFlowCategoryType.Finance,
		_ => Messages.CashFlowCategoryType.Operations
	};

	public override string ToString() => _value ?? "N/A";

	public static CashFlowCategory Parse(string s, IFormatProvider? provider = null) =>
		!TryParse(s, out var result) ? throw new FormatException() : result;

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider,
		[NotNullWhen(true)] out CashFlowCategory? result) {
		for (var i = 0; i < All.Length; i++) {
			if (!string.Equals(All[i]._value, s, StringComparison.OrdinalIgnoreCase)) {
				continue;
			}
			result = All[i];
			return true;
		}
		result = null;
		return false;
	}

	public static bool TryParse([NotNullWhen(true)] string? s, [NotNullWhen(true)] out CashFlowCategory? result)
		=> TryParse(s, null, out result);

	public static CashFlowCategory FromCashFlowCategory(Messages.CashFlowCategoryType value) => value switch {
		Messages.CashFlowCategoryType.Operations => Operations,
		Messages.CashFlowCategoryType.Investment => Investment,
		Messages.CashFlowCategoryType.Finance => Finance,
		_ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
	};
}
