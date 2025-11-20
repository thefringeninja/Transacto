using System.Collections.Immutable;
using System.Text.Json.Serialization;
using NodaTime;
using Transacto.Domain;

namespace Transacto.Plugins.BalanceSheet;

public sealed record BalanceSheetReport(
	LocalDate Thru,
	[property: JsonIgnore] ImmutableArray<BalanceSheetReport.Item> AccountBalances) {
	public bool Equals(BalanceSheetReport? other) {
		if (other is null) return false;
		if (ReferenceEquals(this, other)) return true;
		return Thru == other.Thru &&
		       AccountBalances.SequenceEqual(other.AccountBalances);
	}

	public override int GetHashCode() => HashCode.Combine(
		Thru,
		AccountBalances.Aggregate(397, HashCode.Combine));

	public static BalanceSheetReport For(LocalDate thru) => new(thru, []);

	public record Item(int AccountNumber, decimal Balance);
	
	public decimal GetBalance(int accountNumber) {
		foreach (var (number, balance) in AccountBalances) {
			if (number == accountNumber) return balance;
		}

		return 0;
	}

	public IEnumerable<Item> Assets =>
		AccountBalances.Where(item => Account.For(new AccountNumber(item.AccountNumber)) is AssetAccount);

	public decimal TotalAssets => Assets.Sum(x => x.Balance);

	public IEnumerable<Item> Liabilities =>
		AccountBalances.Where(item => Account.For(new AccountNumber(item.AccountNumber)) is LiabilityAccount);

	public decimal TotalLiabilities => Liabilities.Sum(x => x.Balance);

	public IEnumerable<Item> Equity =>
		AccountBalances.Where(item => Account.For(new AccountNumber(item.AccountNumber)) is EquityAccount);


	public decimal IncomeSummary => AccountBalances
		.Sum(item => Account.For(new AccountNumber(item.AccountNumber)) switch {
			IncomeAccount => item.Balance,
			ExpenseAccount => -item.Balance,
			_ => 0m
		});

	public decimal TotalEquity => IncomeSummary + Equity.Sum(x => x.Balance);

	public decimal TotalEquityAndLiabilities => TotalEquity + TotalLiabilities;
}
