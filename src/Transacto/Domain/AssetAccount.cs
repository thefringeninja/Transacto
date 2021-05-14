namespace Transacto.Domain {
	public record AssetAccount : Account {
		public AssetAccount(AccountName accountName, AccountNumber accountNumber) : base(accountName, accountNumber,
			1000..2000) {
		}

		public override AssetAccount Credit(Money amount) => this with {Balance = Balance - amount};
		public override AssetAccount Debit(Money amount) => this with {Balance = Balance + amount};
	}
}
