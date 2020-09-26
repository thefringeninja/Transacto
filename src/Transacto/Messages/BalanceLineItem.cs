namespace Transacto.Messages {
	public class BalanceLineItem {
		public int AccountNumber { get; set; }
		public decimal Amount { get; set; }

		public void Deconstruct(out int accountNumber, out decimal amount) {
			accountNumber = AccountNumber;
			amount = Amount;
		}
	}
}
