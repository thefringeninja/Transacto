using System;
using Xunit;

namespace Transacto.Domain {
	public class DebitTests {
		[Theory, AutoTransactoData]
		public void Equality(AccountNumber accountNumber, Money amount) {
			var sut = new Debit(accountNumber, amount);
			var copy = new Debit(accountNumber, amount);
			Assert.Equal(sut, copy);
		}

		[Theory, AutoTransactoData]
		public void EqualityOperator(AccountNumber accountNumber, Money amount) {
			var sut = new Debit(accountNumber, amount);
			var copy = new Debit(accountNumber, amount);
			Assert.True(sut == copy);
		}

		[Theory, AutoTransactoData]
		public void InequalityOperator(Debit left, Debit right) {
			Assert.True(left != right);
		}

		[Theory, AutoTransactoData]
		public void MoneyLessThanZeroThrows(AccountNumber accountNumber, decimal value) {
			var amount = new Money(-Math.Abs(value));

			Assert.Throws<ArgumentOutOfRangeException>(() => new Debit(accountNumber, amount));
		}

		[Theory, AutoTransactoData]
		public void ZeroMoney(AccountNumber accountNumber) {
			var sut = new Debit(accountNumber);
			Assert.Equal(new Debit(accountNumber, Money.Zero), sut);
		}

		[Theory, AutoTransactoData]
		public void MoneyAdditionOperator(Debit sut, Money amount) {
			var result = sut + amount;
			Assert.Equal(sut.AccountNumber, result.AccountNumber);
			Assert.Equal(sut.Amount + amount, result.Amount);
		}

		[Theory, AutoTransactoData]
		public void MoneySubtractionOperator(Debit sut) {
			var amount = new Money(sut.Amount.ToDecimal() / 2);
			var result = sut - amount;
			Assert.Equal(sut.AccountNumber, result.AccountNumber);
			Assert.Equal(sut.Amount - amount, result.Amount);
		}


		[Theory, AutoTransactoData]
		public void DecimalAdditionOperator(Debit sut, decimal amount) {
			var result = sut + amount;
			Assert.Equal(sut.AccountNumber, result.AccountNumber);
			Assert.Equal(sut.Amount + amount, result.Amount);
		}

		[Theory, AutoTransactoData]
		public void DecimalSubtractionOperator(Debit sut) {
			var amount = sut.Amount.ToDecimal() / 2;
			var result = sut - amount;
			Assert.Equal(sut.AccountNumber, result.AccountNumber);
			Assert.Equal(sut.Amount - amount, result.Amount);
		}
	}
}
