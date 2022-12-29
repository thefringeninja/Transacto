namespace Transacto.Domain;

public record EquityAccount : Account {
	public EquityAccount(AccountName accountName, AccountNumber accountNumber)
		: base(accountName, accountNumber, 3000..4000) {
	}

	public override EquityAccount Credit(Money amount) => this with { Balance = Balance + amount };
	public override EquityAccount Debit(Money amount) => this with { Balance = Balance - amount };
}
