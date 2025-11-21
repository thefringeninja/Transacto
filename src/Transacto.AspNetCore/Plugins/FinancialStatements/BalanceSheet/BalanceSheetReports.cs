using System.Collections.Immutable;
using Transacto.Domain;

namespace Transacto.Plugins.FinancialStatements.BalanceSheet;

public sealed record BalanceSheetReports(
	ImmutableDictionary<int, string> Accounts,
	ImmutableArray<BalanceSheetReport> Reports) {
	public bool Equals(BalanceSheetReports? other) {
		if (other is null) return false;
		if (ReferenceEquals(this, other)) return true;

		return Reports.SequenceEqual(other.Reports) &&
		       Accounts.OrderBy(a => a.Key).SequenceEqual(other.Accounts.OrderBy(a => a.Key));
	}

	public override int GetHashCode() => HashCode.Combine(
		Reports.Aggregate(397, HashCode.Combine),
		Accounts.Aggregate(397, HashCode.Combine));

	public IEnumerable<int> AssetAccountNumbers =>
		Accounts.Keys.Where(accountNumber => Account.For(new(accountNumber)) is AssetAccount);

	public IEnumerable<int> LiabilityAccountNumbers =>
		Accounts.Keys.Where(accountNumber => Account.For(new(accountNumber)) is LiabilityAccount);

	public IEnumerable<int> EquityAccountNumbers =>
		Accounts.Keys.Where(accountNumber => Account.For(new(accountNumber)) is EquityAccount);

	public IEnumerable<int> IncomeSummaryAccountNumbers =>
		Accounts.Keys.Where(accountNumber => Account.For(new(accountNumber)) is IncomeAccount or AssetAccount);
}