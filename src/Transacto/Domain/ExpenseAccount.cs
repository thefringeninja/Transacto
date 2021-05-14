namespace Transacto.Domain {
	public record ExpenseAccount : Account {
		public ExpenseAccount(AccountName accountName, AccountNumber accountNumber) : base(accountName, accountNumber,
			5000..6000, 6000..7000, 8000..9000) {
		}

		public override ExpenseAccount Credit(Money amount) => this with {Balance = Balance - amount};
		public override ExpenseAccount Debit(Money amount) => this with {Balance = Balance + amount};
	}
}
