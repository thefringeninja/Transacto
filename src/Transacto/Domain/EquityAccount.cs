namespace Transacto.Domain;

public record EquityAccount : Account {
	public EquityAccount(AccountName accountName, AccountNumber accountNumber, AccountNumberWidth accountNumberWidth,
		CashFlowCategory cashFlowCategory) : base(accountName, accountNumber, cashFlowCategory,
		accountNumberSpaces: CreateAccountNumberSpace(3, accountNumberWidth)) {
		CashFlowCategoryMustBe(accountNumber, cashFlowCategory, CashFlowCategory.Finance);
	}

	public override bool IsFictitious => false;

	public override EquityAccount Credit(Money amount) => this with { Balance = Balance + amount };
	public override EquityAccount Debit(Money amount) => this with { Balance = Balance - amount };
}
