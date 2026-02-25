namespace Transacto.Domain;

public record IncomeAccount : Account {
	public IncomeAccount(AccountName accountName, AccountNumber accountNumber, AccountNumberWidth accountNumberWidth,
		CashFlowCategory cashFlowCategory)
		: this(accountName, accountNumber, cashFlowCategory, CreateAccountNumberSpace(4, accountNumberWidth)) {
		CashFlowCategoryMustBe(accountNumber, cashFlowCategory, CashFlowCategory.Operations);
	}

	protected IncomeAccount(AccountName accountName, AccountNumber accountNumber, CashFlowCategory cashFlowCategory,
		Range accountNumberSpace) : base(accountName, accountNumber, cashFlowCategory,
		accountNumberSpaces: accountNumberSpace) {
	}

	public override bool IsFictitious => true;
	public override IncomeAccount Credit(Money amount) => this with { Balance = Balance + amount };
	public override IncomeAccount Debit(Money amount) => this with { Balance = Balance - amount };
}
