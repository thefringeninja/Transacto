using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using Xunit;

namespace Transacto.Domain {
	public class AccountTests {
		private readonly ScenarioFixture _fixture;

		public AccountTests() {
			_fixture = new ScenarioFixture();
		}

		private static IEnumerable<int> GenerateAccountNumbers() {
			var rand = new Random();
			for (var i = 1; i < 9; i++) {
				for (var j = 0; j < 3; j++) {
					yield return rand.Next(i * 1000, (i + 1) * 1000 - 1);
				}
			}
		}

		public static IEnumerable<object[]> CreatesExpectedAccountCases() => GenerateAccountNumbers()
			.Select(i => new object[] {
				new AccountNumber(i),
				i switch {
					>= 1000 and < 2000 => typeof(AssetAccount),
					>= 2000 and < 3000 => typeof(LiabilityAccount),
					>= 3000 and < 4000 => typeof(EquityAccount),
					>= 4000 and < 5000 => typeof(IncomeAccount),
					>= 5000 and < 6000 => typeof(ExpenseAccount),
					>= 6000 and < 7000 => typeof(ExpenseAccount),
					>= 7000 and < 8000 => typeof(IncomeAccount),
					>= 8000 and < 9000 => typeof(ExpenseAccount),
					_ => throw new ArgumentOutOfRangeException()
				}
			});
		[Theory, MemberData(nameof(CreatesExpectedAccountCases))]
		public void CreatesExpectedAccount(AccountNumber accountNumber, Type expected) =>
			Assert.IsType(expected, Account.For(_fixture.Create<AccountName>(), accountNumber));

		public class AssetTests : LeftSideTests<AssetAccount> {}
		public class LiabilityTests : RightSideTests<LiabilityAccount> {}
		public class EquityTests : RightSideTests<LiabilityAccount> {}
		public class IncomeTests : RightSideTests<LiabilityAccount> {}
		public class ExpenseTests : LeftSideTests<ExpenseAccount> {}

		public abstract class LeftSideTests<TAccount> where TAccount : Account {
			[Theory, AutoTransactoData]
			public void DebitsIncreaseBalance(TAccount sut, Money amount) =>
				Assert.Equal(amount, sut.Debit(amount).Balance);

			[Theory, AutoTransactoData]
			public void CreditsDecreaseBalance(TAccount sut, Money amount) =>
				Assert.Equal(-amount, sut.Credit(amount).Balance);
		}

		public abstract class RightSideTests<TAccount> where TAccount : Account {
			[Theory, AutoTransactoData]
			public void CreditsIncreaseBalance(TAccount sut, Money amount) =>
				Assert.Equal(amount, sut.Credit(amount).Balance);

			[Theory, AutoTransactoData]
			public void DebitsDecreaseBalance(TAccount sut, Money amount) =>
				Assert.Equal(-amount, sut.Debit(amount).Balance);
		}
	}
}
