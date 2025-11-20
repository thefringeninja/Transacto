using System.Collections.Immutable;
using System.Text.Json.Serialization;
using NodaTime;
using Transacto.Domain;

namespace Transacto.Plugins.ProfitAndLoss;

public record ProfitAndLossReport(
	ReportRange ReportRange, 
	[property: JsonIgnore] ImmutableArray<ProfitAndLossReport.Item> AccountBalances) {
	public static ProfitAndLossReport For(LocalDate startingOn, Period period) =>
		new(new(startingOn, period), []);

	public record Item(int AccountNumber, decimal Balance);

	public decimal GetBalance(int accountNumber) {
		foreach (var (number, balance) in AccountBalances) {
			if (number == accountNumber) return balance;
		}

		return 0;
	}

	public IEnumerable<Item> GrossRevenue =>
		AccountBalances.Where(item =>
			Account.For(new AccountNumber(item.AccountNumber)) is IncomeAccount and not OtherIncomeAccount);
	
	public decimal TotalGrossRevenue => GrossRevenue.Sum(item => item.Balance);

	public IEnumerable<Item> CostOfRevenue =>
		AccountBalances.Where(item =>
			Account.For(new AccountNumber(item.AccountNumber)) is CostOfRevenueExpenseAccount);
	
	public decimal TotalCostOfRevenue => CostOfRevenue.Sum(item => item.Balance);
	
	public decimal GrossProfit => TotalGrossRevenue - TotalCostOfRevenue;
	
	public IEnumerable<Item> OperatingExpenses =>
		AccountBalances.Where(item =>
			Account.For(new AccountNumber(item.AccountNumber)) is OperatingExpenseAccount);
	
	public decimal TotalOperatingExpenses => OperatingExpenses.Sum(item => item.Balance);

	public decimal OperatingIncome => GrossProfit - TotalOperatingExpenses;
	
	public IEnumerable<Item> OtherExpenses =>
		AccountBalances.Where(item =>
			Account.For(new AccountNumber(item.AccountNumber)) is OtherExpenseAccount);
	
	public decimal TotalOtherExpenses => OtherExpenses.Sum(item => item.Balance);

	public IEnumerable<Item> OtherIncome =>
		AccountBalances.Where(item =>
			Account.For(new AccountNumber(item.AccountNumber)) is OtherIncomeAccount);
	
	public decimal TotalOtherIncome => OtherIncome.Sum(item => item.Balance);

	public decimal NetProfit => OperatingIncome + TotalOtherIncome - TotalOtherExpenses;
}
