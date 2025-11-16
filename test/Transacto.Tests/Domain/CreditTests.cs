namespace Transacto.Domain;

public class CreditTests : ValueObjectTests<Credit> {
	[AutoFixtureData]
	public void MoneyLessThanZeroThrows(AccountNumber accountNumber, decimal value) {
		var amount = new Money(-Math.Abs(value));

		Assert.Throws<ArgumentOutOfRangeException>(() => new Credit(accountNumber, amount));
	}

	[AutoFixtureData]
	public void ZeroMoney(AccountNumber accountNumber) {
		var sut = new Credit(accountNumber);
		Assert.Equal(new Credit(accountNumber, Money.Zero), sut);
	}

	[AutoFixtureData]
	public void MoneyAdditionOperator(Credit sut, Money amount) {
		var result = sut + amount;
		Assert.Equal(sut.AccountNumber, result.AccountNumber);
		Assert.Equal(sut.Amount + amount, result.Amount);
	}

	[AutoFixtureData]
	public void MoneySubtractionOperator(Credit sut) {
		var amount = new Money(sut.Amount.ToDecimal() / 2);
		var result = sut - amount;
		Assert.Equal(sut.AccountNumber, result.AccountNumber);
		Assert.Equal(sut.Amount - amount, result.Amount);
	}


	[AutoFixtureData]
	public void DecimalAdditionOperator(Credit sut, decimal amount) {
		var result = sut + amount;
		Assert.Equal(sut.AccountNumber, result.AccountNumber);
		Assert.Equal(sut.Amount + amount, result.Amount);
	}

	[AutoFixtureData]
	public void DecimalSubtractionOperator(Credit sut) {
		var amount = sut.Amount.ToDecimal() / 2;
		var result = sut - amount;
		Assert.Equal(sut.AccountNumber, result.AccountNumber);
		Assert.Equal(sut.Amount - amount, result.Amount);
	}
}
