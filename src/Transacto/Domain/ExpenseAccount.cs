namespace Transacto.Domain;

public abstract record ExpenseAccount : Account {
	protected ExpenseAccount(AccountName accountName, AccountNumber accountNumber, CashFlowCategory cashFlowCategory,
		Range accountNumberSpace) : base(accountName, accountNumber, cashFlowCategory,
		accountNumberSpaces: accountNumberSpace) {
	}

	public override bool IsFictitious => true;
	public override ExpenseAccount Credit(Money amount) => this with { Balance = Balance - amount };
	public override ExpenseAccount Debit(Money amount) => this with { Balance = Balance + amount };
}
