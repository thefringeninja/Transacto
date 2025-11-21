namespace Transacto.Domain;

public record LiabilityAccount : Account {
	public LiabilityAccount(AccountName accountName, AccountNumber accountNumber, int accountNumberWidth,
		CashFlowCategory cashFlowCategory) : base(accountName, accountNumber, cashFlowCategory,
		accountNumberSpaces: CreateAccountNumberSpace(2, accountNumberWidth)) {
		CashFlowCategoryMustBe(accountNumber, cashFlowCategory, CashFlowCategory.Operations, CashFlowCategory.Finance);
	}

	public override bool IsFictitious => false;
	public override LiabilityAccount Credit(Money amount) => this with { Balance = Balance + amount };
	public override LiabilityAccount Debit(Money amount) => this with { Balance = Balance - amount };
}
