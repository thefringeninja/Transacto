using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Transacto.Domain;

namespace Transacto.Plugins.FinancialStatements.BalanceSheet;

public abstract record Report {
	public string Name { get; }
	public ImmutableDictionary<int, Line> Lines { get; }
	public decimal Total => Lines.Values.Sum(line => line.Balance);
	
	protected Report(string name, ImmutableArray<Line> lines) {
		Name = name;
		Lines = lines.Where(IsMatch).ToImmutableDictionary(line => line.Account.Number);
	}

	protected abstract bool IsMatch(Account account);

	private bool IsMatch(Line line) => IsMatch(Account.For(new(line.Account.Number)));

	[JsonIgnore]
	public ImmutableArray<AccountInfo> Accounts => [
		..Lines.Values.Select(line => line.Account).Distinct()
	];

	public record Line(AccountInfo Account, decimal Balance);

	public record Assets([property: JsonIgnore] ImmutableArray<Line> lines) : Report(nameof(Assets), lines) {
		protected override bool IsMatch(Account account) => account is AssetAccount;
	}

	public record Liabilities([property: JsonIgnore] ImmutableArray<Line> lines) : Report(nameof(Liabilities), lines) {
		protected override bool IsMatch(Account account) => account is LiabilityAccount;
	}

	public record Equity([property: JsonIgnore] ImmutableArray<Line> lines) : Report(nameof(Equity), lines) {
		protected override bool IsMatch(Account account) => account is EquityAccount;
	}
}
