namespace Transacto.Domain;

public record LiabilityAccount : Account {
	public LiabilityAccount(AccountName accountName, AccountNumber accountNumber, int accountNumberWidth)
		: base(accountName, accountNumber, CreateAccountNumberSpace(2, accountNumberWidth)) {
	}

	public override bool IsFictitious => false;
	public override LiabilityAccount Credit(Money amount) => this with { Balance = Balance + amount };
	public override LiabilityAccount Debit(Money amount) => this with { Balance = Balance - amount };
}
