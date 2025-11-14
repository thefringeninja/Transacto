namespace Transacto.Domain;

public class TrialBalanceFailedException(Money balance)
	: Exception($"Expected a balance of {Money.Zero}, current trial balance is {balance}.") {
	public Money Balance { get; } = balance;
}
