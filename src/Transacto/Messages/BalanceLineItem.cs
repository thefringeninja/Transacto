namespace Transacto.Messages;

partial record BalanceLineItem {
	public void Deconstruct(out int accountNumber, out decimal amount) {
		accountNumber = AccountNumber;
		amount = Amount;
	}
}
