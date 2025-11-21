using System.Collections.Immutable;
using Transacto.Messages;

namespace Transacto.Plugins.FinancialStatements.CashFlow;

public sealed record CashFlowReports(
	ImmutableDictionary<int, CashFlowReports.AccountInfo> Accounts,
	ImmutableArray<CashFlowReport> Reports) {
	
	public record AccountInfo(int AccountNumber, string AccountName, DefineAccountCashFlowCategory CashFlowCategory);
	
	public bool Equals(CashFlowReports? other) {
		if (other is null) return false;
		if (ReferenceEquals(this, other)) return true;

		return Reports.SequenceEqual(other.Reports) &&
		       Accounts.OrderBy(a => a.Key).SequenceEqual(other.Accounts.OrderBy(a => a.Key));
	}

	public override int GetHashCode() => HashCode.Combine(
		Reports.Aggregate(397, HashCode.Combine),
		Accounts.Aggregate(397, HashCode.Combine));

	// [JsonIgnore]
	// private IEnumerable<Account> DoesNotAffectCash => Accounts.Values
	// 	.Where(account => !account.AffectsCash)
	// 	.Select(account => Account.For(new(account.AccountNumber), new(account.AccountName),
	// 		CashFlowCategory.FromCashFlowCategory(account.Category)))
	// 	.Where(account => account.IsFictitious);
	//
	public IEnumerable<int> Operations => Accounts.Values
		.Where(account => account.CashFlowCategory == DefineAccountCashFlowCategory.Operations)
		.Select(account => account.AccountNumber);
	
	public IEnumerable<int> Investment => Accounts.Values
		.Where(account => account.CashFlowCategory == DefineAccountCashFlowCategory.Investment)
		.Select(account => account.AccountNumber);
	
	public IEnumerable<int> Finance => Accounts.Values
		.Where(account => account.CashFlowCategory == DefineAccountCashFlowCategory.Finance)
		.Select(account => account.AccountNumber);
}
