using System.Collections.Immutable;
using System.Text.Json.Serialization;
using NodaTime;
using Transacto.Domain;

namespace Transacto.Plugins.BalanceSheet;

public delegate Task<BalanceSheetReports> GetBalanceSheetReportsAsync(
	ImmutableArray<LocalDate> thru,
	CancellationToken ct);

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

public sealed record BalanceSheetReport(
	LocalDate Thru,
	ImmutableArray<BalanceSheetReport.Item> AccountBalances) {
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

	[JsonIgnore]
	public IEnumerable<Item> Assets =>
		AccountBalances.Where(item => Account.For(new AccountNumber(item.AccountNumber)) is AssetAccount);

	public decimal TotalAssets => Assets.Sum(x => x.Balance);

	[JsonIgnore]
	public IEnumerable<Item> Liabilities =>
		AccountBalances.Where(item => Account.For(new AccountNumber(item.AccountNumber)) is LiabilityAccount);

	public decimal TotalLiabilities => Liabilities.Sum(x => x.Balance);

	[JsonIgnore]
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
