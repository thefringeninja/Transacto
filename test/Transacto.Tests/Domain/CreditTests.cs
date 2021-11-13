using System;
using Xunit;

namespace Transacto.Domain; 

public class CreditTests {
	[Theory, AutoTransactoData]
	public void Equality(AccountNumber accountNumber, Money amount) {
		var sut = new Credit(accountNumber, amount);
		var copy = new Credit(accountNumber, amount);
		Assert.Equal(sut, copy);
	}

	[Theory, AutoTransactoData]
	public void EqualityOperator(AccountNumber accountNumber, Money amount) {
		var sut = new Credit(accountNumber, amount);
		var copy = new Credit(accountNumber, amount);
		Assert.True(sut == copy);
	}

	[Theory, AutoTransactoData]
	public void InequalityOperator(Credit left, Credit right) {
		Assert.True(left != right);
	}

	[Theory, AutoTransactoData]
	public void MoneyLessThanZeroThrows(AccountNumber accountNumber, decimal value) {
		var amount = new Money(-Math.Abs(value));

		Assert.Throws<ArgumentOutOfRangeException>(() => new Credit(accountNumber, amount));
	}

	[Theory, AutoTransactoData]
	public void ZeroMoney(AccountNumber accountNumber) {
		var sut = new Credit(accountNumber);
		Assert.Equal(new Credit(accountNumber, Money.Zero), sut);
	}

	[Theory, AutoTransactoData]
	public void MoneyAdditionOperator(Credit sut, Money amount) {
		var result = sut + amount;
		Assert.Equal(sut.AccountNumber, result.AccountNumber);
		Assert.Equal(sut.Amount + amount, result.Amount);
	}

	[Theory, AutoTransactoData]
	public void MoneySubtractionOperator(Credit sut) {
		var amount = new Money(sut.Amount.ToDecimal() / 2);
		var result = sut - amount;
		Assert.Equal(sut.AccountNumber, result.AccountNumber);
		Assert.Equal(sut.Amount - amount, result.Amount);
	}
		
		
	[Theory, AutoTransactoData]
	public void DecimalAdditionOperator(Credit sut, decimal amount) {
		var result = sut + amount;
		Assert.Equal(sut.AccountNumber, result.AccountNumber);
		Assert.Equal(sut.Amount + amount, result.Amount);
	}

	[Theory, AutoTransactoData]
	public void DecimalSubtractionOperator(Credit sut) {
		var amount = sut.Amount.ToDecimal() / 2;
		var result = sut - amount;
		Assert.Equal(sut.AccountNumber, result.AccountNumber);
		Assert.Equal(sut.Amount - amount, result.Amount);
	}
}