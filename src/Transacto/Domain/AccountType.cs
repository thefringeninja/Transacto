using System;
using System.Collections.Generic;

namespace Transacto.Domain {
	public abstract class AccountType {
		public static readonly AccountType Asset = new AssetAccount();
		public static readonly AccountType Liability = new LiabilityAccount();
		public static readonly AccountType Equity = new EquityAccount();
		public static readonly AccountType Income = new IncomeAccount();
		public static readonly AccountType CostOfGoodsSold = new ExpenseAccount();
		public static readonly AccountType Expenses = new ExpenseAccount();
		public static readonly AccountType OtherIncome = new IncomeAccount();
		public static readonly AccountType OtherExpenses = new ExpenseAccount();

		public static readonly IReadOnlyList<AccountType> All = new[] {
			Asset, Liability, Equity, Income, CostOfGoodsSold, Expenses, OtherIncome, OtherExpenses
		};

		public string Name { get; }
		public abstract bool AppearsOnBalanceSheet { get; }
		public abstract bool AppearsOnProfitAndLoss { get; }

		public static AccountType OfAccountNumber(AccountNumber value) => value.Value switch {
			>= 1000 and < 2000 => Asset,
			>= 2000 and < 3000 => Liability,
			>= 3000 and < 4000 => Equity,
			>= 4000 and < 5000 => Income,
			>= 5000 and < 6000 => CostOfGoodsSold,
			>= 6000 and < 7000 => Expenses,
			>= 7000 and < 8000 => OtherIncome,
			>= 8000 and < 9000 => OtherExpenses,
			_ => throw new ArgumentOutOfRangeException(nameof(value))
		};

		protected AccountType() {
			Name = GetType().Name;
		}

		public void MustBe(AccountType other) {
			if (this != other) {
				throw new InvalidAccountTypeException(other, this);
			}
		}

		public class AssetAccount : AccountType {
			public override bool AppearsOnBalanceSheet { get; } = true;
			public override bool AppearsOnProfitAndLoss { get; } = false;
		}

		public class LiabilityAccount : AccountType {
			public override bool AppearsOnBalanceSheet { get; } = true;
			public override bool AppearsOnProfitAndLoss { get; } = false;
		}

		public class EquityAccount : AccountType {
			public override bool AppearsOnBalanceSheet { get; } = true;
			public override bool AppearsOnProfitAndLoss { get; } = false;
		}

		public class IncomeAccount : AccountType {
			public override bool AppearsOnBalanceSheet { get; } = false;
			public override bool AppearsOnProfitAndLoss { get; } = true;
		}

		public class ExpenseAccount : AccountType {
			public override bool AppearsOnBalanceSheet { get; } = false;
			public override bool AppearsOnProfitAndLoss { get; } = true;
		}
	}
}
