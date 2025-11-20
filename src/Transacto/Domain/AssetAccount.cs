namespace Transacto.Domain;

public record AssetAccount : Account {
	public AssetAccount(AccountName accountName, AccountNumber accountNumber, int accountNumberWidth) :
		base(accountName, accountNumber, CreateAccountNumberSpace(1, accountNumberWidth)) {
	}

	public override bool IsFictitious => false;

	public override AssetAccount Credit(Money amount) => this with { Balance = Balance - amount };
	public override AssetAccount Debit(Money amount) => this with { Balance = Balance + amount };
}
