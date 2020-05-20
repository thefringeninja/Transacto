using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Transacto.Domain {
	public class AccountTypeTests {
		[Theory, AutoTransactoData]
		public void Equality(AccountNumber accountNumber) {
			Assert.Equal(AccountType.OfAccountNumber(accountNumber), AccountType.OfAccountNumber(accountNumber));
		}

		[Theory, AutoTransactoData]
		public void EqualityOperator(AccountNumber accountNumber) {
			Assert.True(AccountType.OfAccountNumber(accountNumber) == AccountType.OfAccountNumber(accountNumber));
		}

		[Theory, AutoTransactoData]
		public void InequalityOperator(AccountNumber accountNumber) {
			Assert.False(AccountType.OfAccountNumber(accountNumber) != AccountType.OfAccountNumber(accountNumber));
		}

		public static IEnumerable<object[]> AppearsOnBalanceSheetCases() {
			yield return new object[] {AccountType.Asset, true};
			yield return new object[] {AccountType.Liability, true};
			yield return new object[] {AccountType.Equity, true};
			yield return new object[] {AccountType.Income, false};
			yield return new object[] {AccountType.CostOfGoodsSold, false};
			yield return new object[] {AccountType.Expenses, false};
			yield return new object[] {AccountType.OtherIncome, false};
			yield return new object[] {AccountType.OtherExpenses, false};
		}

		[Theory, MemberData(nameof(AppearsOnBalanceSheetCases))]
		public void AppearsOnBalanceSheet(AccountType sut, bool appearsOnBalanceSheet) {
			Assert.Equal(appearsOnBalanceSheet, sut.AppearsOnBalanceSheet);
		}

		public static IEnumerable<object[]> AppearsOnProfitAndLossCases() {
			yield return new object[] {AccountType.Asset, false};
			yield return new object[] {AccountType.Liability, false};
			yield return new object[] {AccountType.Equity, false};
			yield return new object[] {AccountType.Income, true};
			yield return new object[] {AccountType.CostOfGoodsSold, true};
			yield return new object[] {AccountType.Expenses, true};
			yield return new object[] {AccountType.OtherIncome, true};
			yield return new object[] {AccountType.OtherExpenses, true};
		}

		[Theory, MemberData(nameof(AppearsOnProfitAndLossCases))]
		public void AppearsOnProfitAndLoss(AccountType sut, bool appearsOnProfitAndLoss) {
			Assert.Equal(appearsOnProfitAndLoss, sut.AppearsOnProfitAndLoss);
		}

		[Theory, AutoTransactoData]
		public void ToStringReturnsExpectedResult(AccountType accountType) {
			var expected = accountType.GetType().Name;
			Assert.Equal(expected, accountType.Name);
		}

		[Theory, AutoTransactoData]
		public void MustBe(AccountType accountType) {
			accountType.MustBe(accountType);
		}

		[Theory, AutoTransactoData]
		public void MustBeDoesNotMatchThrows(Random random) {
			var index = random.Next(0, AccountType.All.Count);
			var sut = AccountType.All[index];
			var other = AccountType.All[(index + 1) % AccountType.All.Count];
			var ex = Assert.Throws<InvalidAccountTypeException>(() => sut.MustBe(other));
			Assert.Equal(other, ex.Expected);
			Assert.Equal(sut, ex.Actual);
			Assert.Equal($"Expected an account type of '{ex.Expected.Name}', received '{ex.Actual.Name}'.", ex.Message);
		}
	}
}
