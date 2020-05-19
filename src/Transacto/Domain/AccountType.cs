using System;

namespace Transacto.Domain {
	public class AccountType {
		public static readonly AccountType Asset = new AssetAccount();
		public static readonly AccountType Liability = new LiabilityAccount();
		public static readonly AccountType Equity = new EquityAccount();
		public static readonly AccountType Income = new IncomeAccount();
		public static readonly AccountType CostOfGoodsSold = new ExpenseAccount();
		public static readonly AccountType Expenses = new ExpenseAccount();
		public static readonly AccountType OtherIncome = new IncomeAccount();
		public static readonly AccountType OtherExpenses = new ExpenseAccount();
		public bool AppearsOnBalanceSheet => this is AssetAccount || this is LiabilityAccount || this is EquityAccount;
		public bool AppearsOnProfitAndLoss => this is IncomeAccount || this is ExpenseAccount;

		public static AccountType OfAccountNumber(AccountNumber value) => value switch {
			var x when x.Value >= 1000 && x.Value < 2000 => Asset,
			var x when x.Value >= 2000 && x.Value < 3000 => Liability,
			var x when x.Value >= 3000 && x.Value < 4000 => Equity,
			var x when x.Value >= 4000 && x.Value < 5000 => Income,
			var x when x.Value >= 5000 && x.Value < 6000 => CostOfGoodsSold,
			var x when x.Value >= 6000 && x.Value < 7000 => Expenses,
			var x when x.Value >= 7000 && x.Value < 8000 => OtherIncome,
			var x when x.Value >= 8000 && x.Value < 9000 => OtherExpenses,
			_ => throw new ArgumentOutOfRangeException(nameof(value))
		};

		public void MustBe(AccountType other) {
			if (this != other) {
				throw new InvalidOperationException();
			}
		}

		public class AssetAccount : AccountType {
		}

		public class LiabilityAccount : AccountType {
		}

		public class EquityAccount : AccountType {
		}

		public class IncomeAccount : AccountType {
		}

		public class ExpenseAccount : AccountType {
		}
	}
}
