namespace Transacto.Domain;

public record ChartOfAccountsSettings(
	int AccountNumberWidth,
	AccountNumber AccountsReceivable,
	AccountNumber AccountsPayable,
	AccountNumber RetainedEarnings) {
	public const int MinAccountNumberWidth = 4;
	public const int MaxAccountNumberWidth = 8;

	public static readonly ChartOfAccountsSettings Default = new(MinAccountNumberWidth, new AccountNumber(1100),
		new AccountNumber(2000), new AccountNumber(3900));

	public int AccountNumberWidth { get; } = ValidateAccountNumberWidth(AccountNumberWidth);

	public AccountNumber AccountsReceivable { get; } = ValidateAccountsReceivable(AccountsReceivable);
	public AccountNumber AccountsPayable { get; } = ValidateAccountsPayable(AccountsPayable);
	public  AccountNumber RetainedEarnings { get; } = ValidateRetainedEarnings(RetainedEarnings);

	private static int ValidateAccountNumberWidth(int accountNumberWidth) {
		ArgumentOutOfRangeException.ThrowIfLessThan(accountNumberWidth, MinAccountNumberWidth);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(accountNumberWidth, MaxAccountNumberWidth);
		return accountNumberWidth;
	}

	private static AccountNumber ValidateAccountsReceivable(AccountNumber accountNumber) {
		ArgumentException.ThrowIfAccountIsNot<AssetAccount>(accountNumber);
		return accountNumber;
	}

	private static AccountNumber ValidateAccountsPayable(AccountNumber accountNumber) {
		ArgumentException.ThrowIfAccountIsNot<LiabilityAccount>(accountNumber);
		return accountNumber;
	}

	private static AccountNumber ValidateRetainedEarnings(AccountNumber accountNumber) {
		ArgumentException.ThrowIfAccountIsNot<EquityAccount>(accountNumber);
		return accountNumber;
	}
};
