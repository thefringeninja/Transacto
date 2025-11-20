namespace Transacto.Domain;

public record EquityAccount : Account {
	public EquityAccount(AccountName accountName, AccountNumber accountNumber, int accountNumberWidth)
		: base(accountName, accountNumber, CreateAccountNumberSpace(3, accountNumberWidth)) {
	}

	public override bool IsFictitious => false;
	
	public override EquityAccount Credit(Money amount) => this with { Balance = Balance + amount };
	public override EquityAccount Debit(Money amount) => this with { Balance = Balance - amount };
}
