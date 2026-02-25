namespace Transacto.Domain;

public record ChartOfAccountsSettings(
	AccountNumberWidth AccountNumberWidth,
	AccountNumber AccountsReceivable,
	AccountNumber AccountsPayable,
	AccountNumber RetainedEarnings) {
	public static readonly ChartOfAccountsSettings Default = new(AccountNumberWidth.Min, new AccountNumber(1100),
		new AccountNumber(2000), new AccountNumber(3900));

	public AccountNumber AccountsReceivable { get; } = ValidateAccountsReceivable(AccountsReceivable);
	public AccountNumber AccountsPayable { get; } = ValidateAccountsPayable(AccountsPayable);
	public  AccountNumber RetainedEarnings { get; } = ValidateRetainedEarnings(RetainedEarnings);

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
