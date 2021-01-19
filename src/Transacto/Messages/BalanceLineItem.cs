namespace Transacto.Messages {
	public record BalanceLineItem {
		public int AccountNumber;
		public decimal Amount;

		public void Deconstruct(out int accountNumber, out decimal amount) {
			accountNumber = AccountNumber;
			amount = Amount;
		}
	}
}
