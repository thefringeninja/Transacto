using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Transacto.Domain;
using Transacto.Messages;

namespace Transacto.Plugins.FinancialStatements.CashFlow;

public record CashFlowReport(
	ReportRange ReportRange,
	decimal StartingCash,
	[property: JsonIgnore] ImmutableArray<CashFlowReport.AccountBalance> AccountBalances) {
	public record AccountBalance(
		int AccountNumber,
		decimal Balance,
		DefineAccountCashFlowCategory CashFlowCategory);

	public IEnumerable<AccountBalance> Operations =>
		GetCashFlowItemsOfCategory(DefineAccountCashFlowCategory.Operations);

	public IEnumerable<AccountBalance> Investment =>
		GetCashFlowItemsOfCategory(DefineAccountCashFlowCategory.Investment);

	public IEnumerable<AccountBalance> Finance => GetCashFlowItemsOfCategory(DefineAccountCashFlowCategory.Finance);

	private IEnumerable<AccountBalance> GetCashFlowItemsOfCategory(DefineAccountCashFlowCategory cashFlowCategory) =>
		AccountBalances.Where(item => item.CashFlowCategory == cashFlowCategory);

	public decimal NetCashFromOperations => GetBalanceOfCategory(DefineAccountCashFlowCategory.Operations);

	public decimal NetCashFromInvestment => GetBalanceOfCategory(DefineAccountCashFlowCategory.Investment);

	public decimal NetCashFromFinancing => GetBalanceOfCategory(DefineAccountCashFlowCategory.Finance);

	public decimal CashFlow => NetCashFromOperations + NetCashFromInvestment + NetCashFromFinancing;

	public decimal EndingCash => StartingCash + CashFlow;

	private decimal GetBalanceOfCategory(DefineAccountCashFlowCategory cashFlowCategory) =>
		GetCashFlowItemsOfCategory(cashFlowCategory)
			.Sum(account => CalculateBalance(account.AccountNumber, account.Balance));

	private static decimal CalculateBalance(int accountNumber, decimal balance) {
		return Account.For(new(accountNumber)) switch {
			AssetAccount => -balance,
			LiabilityAccount => balance,
			_ => 0m
		};
	}

	public decimal GetBalance(int accountNumber) {
		foreach (var (number, balance, _) in AccountBalances) {
			if (number == accountNumber) return CalculateBalance(accountNumber, balance);
		}

		return 0;
	}
}
