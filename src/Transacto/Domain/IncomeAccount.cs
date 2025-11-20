namespace Transacto.Domain;

public record IncomeAccount : Account {
	public IncomeAccount(AccountName accountName, AccountNumber accountNumber, int accountNumberWidth)
		: this(accountName, accountNumber, CreateAccountNumberSpace(4, accountNumberWidth)) {
	}

	protected IncomeAccount(AccountName accountName, AccountNumber accountNumber, Range accountNumberSpace)
		: base(accountName, accountNumber, accountNumberSpace) {
	}

	public override bool IsFictitious => true;
	public override IncomeAccount Credit(Money amount) => this with { Balance = Balance + amount };
	public override IncomeAccount Debit(Money amount) => this with { Balance = Balance - amount };
}
