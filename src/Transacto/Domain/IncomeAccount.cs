namespace Transacto.Domain;

public record IncomeAccount : Account {
	public IncomeAccount(AccountName accountName, AccountNumber accountNumber) : base(accountName, accountNumber,
		4000..5000, 7000..8000) {
	}

	public override IncomeAccount Credit(Money amount) => this with { Balance = Balance + amount };
	public override IncomeAccount Debit(Money amount) => this with { Balance = Balance - amount };
}
