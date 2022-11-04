namespace Transacto.Messages; 

public record BalanceLineItem {
	public int AccountNumber { get; init; }
	public decimal Amount { get; init; }

	public void Deconstruct(out int accountNumber, out decimal amount) {
		accountNumber = AccountNumber;
		amount = Amount;
	}
}