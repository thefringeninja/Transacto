namespace Transacto.Domain;

public record AssetAccount : Account {
	public AssetAccount(AccountName accountName, AccountNumber accountNumber, AccountNumberWidth accountNumberWidth,
		CashFlowCategory cashFlowCategory, bool affectsCash) : base(accountName, accountNumber, cashFlowCategory,
		affectsCash, CreateAccountNumberSpace(1, accountNumberWidth)) {
		if (affectsCash) {
			CashFlowCategoryMustBe(accountNumber, cashFlowCategory, CashFlowCategory.Operations);
		}
	}

	public override bool IsFictitious => false;

	public override AssetAccount Credit(Money amount) => this with { Balance = Balance - amount };
	public override AssetAccount Debit(Money amount) => this with { Balance = Balance + amount };
}
