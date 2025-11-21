namespace Transacto.Domain;

public abstract record Account {
	public AccountName AccountName { get; }
	public AccountNumber AccountNumber { get; }
	public Money Balance { get; protected init; } = Money.Zero;

	public CashFlowCategory CashFlowCategory { get; }
	public bool AffectsCash { get; }

	public abstract bool IsFictitious { get; }
	public bool IsReal => !IsFictitious;

	public static Account For(AccountNumber accountNumber, AccountName accountName = default,
		CashFlowCategory? cashFlowCategory = null, bool affectsCash = false, ChartOfAccountsSettings? settings = null) {
		var accountNumberString = accountNumber.ToString();

		if (settings is not null && accountNumberString.Length != settings.AccountNumberWidth) {
			throw new AccountNumberLengthOutOfRangeException(accountNumber, settings.AccountNumberWidth,
				nameof(accountNumber));
		}

		var accountType = int.Parse([accountNumberString[0]]);

		var accountNumberWidth = settings?.AccountNumberWidth ?? accountNumberString.Length;

		cashFlowCategory ??= CashFlowCategory.None;

		return (accountType, affectsCash) switch {
			(1, _) => new AssetAccount(accountName, accountNumber, accountNumberWidth, cashFlowCategory, affectsCash),
			(2, false) => new LiabilityAccount(accountName, accountNumber, accountNumberWidth, cashFlowCategory),
			(3, false) => new EquityAccount(accountName, accountNumber, accountNumberWidth, cashFlowCategory),
			(4, false) => new IncomeAccount(accountName, accountNumber, accountNumberWidth, cashFlowCategory),
			(5, false) => new CostOfRevenueExpenseAccount(accountName, accountNumber, accountNumberWidth,
				cashFlowCategory),
			(6, false) => new OperatingExpenseAccount(accountName, accountNumber, accountNumberWidth, cashFlowCategory),
			(7, false) => new OtherIncomeAccount(accountName, accountNumber, accountNumberWidth, cashFlowCategory),
			(8, false) => new OtherExpenseAccount(accountName, accountNumber, accountNumberWidth, cashFlowCategory),
			(_, true) => throw new AccountCannotBeCashAccountException(accountNumber),
			_ => throw new ArgumentOutOfRangeException(nameof(accountNumber))
		};
	}

	// ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
	protected Account(AccountName accountName, AccountNumber accountNumber, CashFlowCategory cashFlowCategory,
		bool affectsCash = false, params Span<Range> accountNumberSpaces) {
		ArgumentNullException.ThrowIfNull(cashFlowCategory);
		if (accountNumberSpaces.Length == 0) {
			throw new ArgumentException("No valid account number range was specified.",
				nameof(accountNumberSpaces));
		}

		AffectsCash = affectsCash;
		CashFlowCategory = cashFlowCategory;

		foreach (var space in accountNumberSpaces) {
			if (accountNumber.Value < space.Start.Value || accountNumber.Value >= space.End.Value) {
				continue;
			}

			AccountName = accountName;
			AccountNumber = accountNumber;
			return;
		}

		throw new ArgumentOutOfRangeException(nameof(accountNumber));
	}

	public abstract Account Credit(Money amount);
	public abstract Account Debit(Money amount);

	protected static void CashFlowCategoryMustBe(AccountNumber accountNumber, CashFlowCategory cashFlowCategory, 
		params Span<CashFlowCategory> expected) {
		if (cashFlowCategory == CashFlowCategory.None) {
			return;
		}

		if (expected.Contains(cashFlowCategory)) {
			return;
		}
		
		throw new AccountExpectedCashFlowCategoryException(accountNumber, cashFlowCategory, expected.ToArray());
	}

	protected static Range CreateAccountNumberSpace(int accountType, int accountNumberWidth) {
		return new Range(
			Create(accountType, accountNumberWidth),
			Create(accountType + 1, accountNumberWidth));

		int Create(int startDigit, int accountNumberWidth) {
			int zeroCount = accountNumberWidth - 1;
			int powerOfTen = 1;
			for (int i = 0; i < zeroCount; i++) {
				powerOfTen *= 10;
			}

			return startDigit * powerOfTen;
		}
	}
}
