namespace Transacto.Domain {
	public record LiabilityAccount : Account {
		public LiabilityAccount(AccountName accountName, AccountNumber accountNumber)
			: base(accountName, accountNumber, 2000..3000) {
		}

		public override LiabilityAccount Credit(Money amount) => this with {Balance = Balance + amount};
		public override LiabilityAccount Debit(Money amount) => this with {Balance = Balance - amount};
	}
}
