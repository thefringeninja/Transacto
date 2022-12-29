namespace Transacto.Messages;

public record BalanceLineItem {
	public required int AccountNumber { get; init; }
	public required decimal Amount { get; init; }

	public void Deconstruct(out int accountNumber, out decimal amount) {
		accountNumber = AccountNumber;
		amount = Amount;
	}
}
