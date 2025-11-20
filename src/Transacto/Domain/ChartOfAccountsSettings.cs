namespace Transacto.Domain;

public record ChartOfAccountsSettings(int AccountNumberWidth) {
	public const int MinAccountNumberWidth = 4;
	public const int MaxAccountNumberWidth = 8;

	public static ChartOfAccountsSettings Default = new(MinAccountNumberWidth);
	public int AccountNumberWidth { get; } = ValidateAccountNumberWidth(AccountNumberWidth);
	
	private static int ValidateAccountNumberWidth(int accountNumberWidth) {
		ArgumentOutOfRangeException.ThrowIfLessThan(accountNumberWidth, MinAccountNumberWidth);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(accountNumberWidth, MaxAccountNumberWidth);
		return accountNumberWidth;
	}
};
