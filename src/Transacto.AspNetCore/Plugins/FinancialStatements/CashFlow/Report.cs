using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Transacto.Domain;
using Transacto.Messages;
using Transacto.Plugins.FinancialStatements;
namespace Transacto.Plugins.FinancialStatements.CashFlow;

public abstract record Report {
	public ImmutableDictionary<int, Line> Lines { get; }

	public string Name { get; }

	protected Report(string name, ImmutableArray<Line> lines) {
		Name = name;
		Lines = lines
			.Where(IsMatch)
			.Select(line => line with {
				Balance = Account.For(new(line.Account.Number)) switch {
					AssetAccount => -line.Balance,
					LiabilityAccount => line.Balance,
					_ => 0m
				}
			})
			.ToImmutableDictionary(line => line.Account.Number);
	}

	public decimal Total => Lines.Values.Sum(item => item.Balance);

	protected abstract bool IsMatch(Account account);

	private bool IsMatch(Line line) => IsMatch(Account.For(new(line.Account.Number),
		cashFlowCategory: line.CashFlowCategory switch {
			CashFlowCategoryType.Investment => CashFlowCategory.Investment,
			CashFlowCategoryType.Finance => CashFlowCategory.Finance,
			_ => CashFlowCategory.Operations,
		}));

	[JsonIgnore]
	public ImmutableArray<AccountInfo> Accounts => [
		..Lines.Values.Select(line => line.Account).Distinct()
	];

	public record Line(AccountInfo Account, decimal Balance, CashFlowCategoryType CashFlowCategory);

	public record Operations([property: JsonIgnore] ImmutableArray<Line> lines) : Report(nameof(Operations), lines) {
		protected override bool IsMatch(Account account) => account.CashFlowCategory == CashFlowCategory.Operations &&
		                                                    account.IsReal;
	}

	public record Investment([property: JsonIgnore] ImmutableArray<Line> lines) : Report(nameof(Investment), lines) {
		protected override bool IsMatch(Account account) => account.CashFlowCategory == CashFlowCategory.Investment &&
		                                                    account.IsReal;
	}

	public record Finance([property: JsonIgnore] ImmutableArray<Line> lines) : Report(nameof(Finance), lines) {
		protected override bool IsMatch(Account account) => account.CashFlowCategory == CashFlowCategory.Finance &&
		                                                    account.IsReal;
	}
}
