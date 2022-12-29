namespace Transacto.Domain;

public class DebitTests {
	[AutoFixtureData]
	public void Equality(AccountNumber accountNumber, Money amount) {
		var sut = new Debit(accountNumber, amount);
		var copy = new Debit(accountNumber, amount);
		Assert.Equal(sut, copy);
	}

	[AutoFixtureData]
	public void EqualityOperator(AccountNumber accountNumber, Money amount) {
		var sut = new Debit(accountNumber, amount);
		var copy = new Debit(accountNumber, amount);
		Assert.True(sut == copy);
	}

	[AutoFixtureData]
	public void InequalityOperator(Debit left, Debit right) {
		Assert.True(left != right);
	}

	[AutoFixtureData]
	public void MoneyLessThanZeroThrows(AccountNumber accountNumber, decimal value) {
		var amount = new Money(-Math.Abs(value));

		Assert.Throws<ArgumentOutOfRangeException>(() => new Debit(accountNumber, amount));
	}

	[AutoFixtureData]
	public void ZeroMoney(AccountNumber accountNumber) {
		var sut = new Debit(accountNumber);
		Assert.Equal(new Debit(accountNumber, Money.Zero), sut);
	}

	[AutoFixtureData]
	public void MoneyAdditionOperator(Debit sut, Money amount) {
		var result = sut + amount;
		Assert.Equal(sut.AccountNumber, result.AccountNumber);
		Assert.Equal(sut.Amount + amount, result.Amount);
	}

	[AutoFixtureData]
	public void MoneySubtractionOperator(Debit sut) {
		var amount = new Money(sut.Amount.ToDecimal() / 2);
		var result = sut - amount;
		Assert.Equal(sut.AccountNumber, result.AccountNumber);
		Assert.Equal(sut.Amount - amount, result.Amount);
	}


	[AutoFixtureData]
	public void DecimalAdditionOperator(Debit sut, decimal amount) {
		var result = sut + amount;
		Assert.Equal(sut.AccountNumber, result.AccountNumber);
		Assert.Equal(sut.Amount + amount, result.Amount);
	}

	[AutoFixtureData]
	public void DecimalSubtractionOperator(Debit sut) {
		var amount = sut.Amount.ToDecimal() / 2;
		var result = sut - amount;
		Assert.Equal(sut.AccountNumber, result.AccountNumber);
		Assert.Equal(sut.Amount - amount, result.Amount);
	}
}
