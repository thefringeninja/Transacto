namespace Transacto.Domain;

public abstract record Account {
	public AccountName AccountName { get; }
	public AccountNumber AccountNumber { get; }
	public Money Balance { get; protected init; } = Money.Zero;

	public abstract bool IsFictitious { get; }
	public bool IsReal => !IsFictitious;

	public static Account For(AccountNumber accountNumber, AccountName accountName = default,
		ChartOfAccountsSettings? settings = null) {
		var accountNumberString = accountNumber.ToString();

		if (settings is not null && accountNumberString.Length != settings.AccountNumberWidth) {
			throw new AccountNumberLengthOutOfRangeException(accountNumber, settings.AccountNumberWidth,
				nameof(accountNumber));
		}

		var accountType = int.Parse([accountNumberString[0]]);
		
		var accountNumberWidth = settings?.AccountNumberWidth ?? accountNumberString.Length;

		return accountType switch {
			1 => new AssetAccount(accountName, accountNumber, accountNumberWidth),
			2 => new LiabilityAccount(accountName, accountNumber, accountNumberWidth),
			3 => new EquityAccount(accountName, accountNumber, accountNumberWidth),
			4 => new IncomeAccount(accountName, accountNumber, accountNumberWidth),
			5 => new CostOfRevenueExpenseAccount(accountName, accountNumber, accountNumberWidth),
			6 => new OperatingExpenseAccount(accountName, accountNumber, accountNumberWidth),
			7 => new OtherIncomeAccount(accountName, accountNumber, accountNumberWidth),
			8 => new OtherExpenseAccount(accountName, accountNumber, accountNumberWidth),
			_ => throw new ArgumentOutOfRangeException(nameof(accountNumber))
		};
	}

	// ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
	protected Account(AccountName accountName, AccountNumber accountNumber, params Span<Range> accountNumberSpaces) {
		if (accountNumberSpaces.Length == 0) {
			throw new ArgumentException("No valid account number range was specified.",
				nameof(accountNumberSpaces));
		}

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
