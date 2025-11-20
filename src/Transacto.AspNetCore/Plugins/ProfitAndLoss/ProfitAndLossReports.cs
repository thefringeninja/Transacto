using System.Collections.Immutable;
using Transacto.Domain;

namespace Transacto.Plugins.ProfitAndLoss;

public sealed record ProfitAndLossReports(
	ImmutableDictionary<int, string> Accounts,
	ImmutableArray<ProfitAndLossReport> Reports) {
	public static readonly ProfitAndLossReports Empty = new(ImmutableDictionary<int, string>.Empty, []);

	public bool Equals(ProfitAndLossReports? other) {
		if (other is null) return false;
		if (ReferenceEquals(this, other)) return true;

		return Reports.SequenceEqual(other.Reports) &&
		       Accounts.OrderBy(a => a.Key).SequenceEqual(other.Accounts.OrderBy(a => a.Key));
	}

	public override int GetHashCode() => HashCode.Combine(
		Reports.Aggregate(397, HashCode.Combine),
		Accounts.Aggregate(397, HashCode.Combine));
	
	public IEnumerable<int> GrossRevenueAccountNumbers =>
		Accounts.Keys.Where(accountNumber => Account.For(new(accountNumber)) is IncomeAccount and not OtherIncomeAccount);

	public IEnumerable<int> CostOfRevenueAccountNumbers =>
		Accounts.Keys.Where(accountNumber => Account.For(new(accountNumber)) is CostOfRevenueExpenseAccount);

	public IEnumerable<int> OperatingExpenseAccountNumbers =>
		Accounts.Keys.Where(accountNumber => Account.For(new(accountNumber)) is OperatingExpenseAccount);
	
	public IEnumerable<int> OtherExpenseAccountNumbers =>
		Accounts.Keys.Where(accountNumber => Account.For(new(accountNumber)) is OtherExpenseAccount);

	public IEnumerable<int> OtherIncomeAccountNumbers =>
		Accounts.Keys.Where(accountNumber => Account.For(new(accountNumber)) is OtherIncomeAccount);
}
