using System;

namespace Transacto.Domain; 

public class TrialBalanceFailedException : Exception {
	public Money Balance { get; }

	public TrialBalanceFailedException(Money balance) : base(
		$"Expected a balance of {Money.Zero}, current trial balance is {balance}.") {
		Balance = balance;
	}
}