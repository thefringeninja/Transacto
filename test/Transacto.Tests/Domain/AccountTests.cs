using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;

namespace Transacto.Domain;

public class AccountTests {
	private readonly ScenarioFixture _fixture = new();

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
				>= 5000 and < 6000 => typeof(CostOfRevenueExpenseAccount),
				>= 6000 and < 7000 => typeof(OperatingExpenseAccount),
				>= 7000 and < 8000 => typeof(OtherIncomeAccount),
				>= 8000 and < 9000 => typeof(OtherExpenseAccount),
				_ => throw new ArgumentOutOfRangeException()
			}
		});

	[MemberData(nameof(CreatesExpectedAccountCases))]
	public void CreatesExpectedAccount(AccountNumber accountNumber, Type expected) =>
		Assert.IsType(expected, Account.For(accountNumber, _fixture.Create<AccountName>()));

	public static IEnumerable<object[]> CannotBeCashAccountCases() => GenerateAccountNumbers()
		.Where(an => an >= 2000)
		.Select(an => (object[])[new AccountNumber(an)]);

	[MemberData(nameof(CannotBeCashAccountCases))]
	public void NonAssetAccountsAffectingThrow(AccountNumber accountNumber)
		=> Assert.Throws<AccountCannotBeCashAccountException>(() =>
			Account.For(accountNumber, _fixture.Create<AccountName>(), affectsCash: true));


	// ReSharper disable once UnusedType.Global
	public class ShortTermCashAssetAccountTests()
		: LeftSideTests<AssetAccount>(true, CashFlowCategory.Investment, CashFlowCategory.Finance);

	// ReSharper disable once UnusedType.Global
	public class NonCashAssetAccountTests()
		: LeftSideTests<AssetAccount>();

	// ReSharper disable once UnusedType.Global
	public class LiabilityAccountTests()
		: RightSideTests<LiabilityAccount>(false, CashFlowCategory.Investment);

	// ReSharper disable once UnusedType.Global
	public class EquityAccountTests()
		: RightSideTests<EquityAccount>(false, CashFlowCategory.Operations, CashFlowCategory.Investment);

	// ReSharper disable once UnusedType.Global
	public class IncomeAccountTests()
		: RightSideTests<IncomeAccount>(false, CashFlowCategory.Investment, CashFlowCategory.Finance);

	// ReSharper disable once UnusedType.Global
	public class CostOfRevenueExpenseAccountTests()
		: LeftSideTests<CostOfRevenueExpenseAccount>(false, CashFlowCategory.Investment, CashFlowCategory.Finance);

	// ReSharper disable once UnusedType.Global
	public class OperatingExpenseAccountTests() : LeftSideTests<OperatingExpenseAccount>(false, CashFlowCategory.Investment, CashFlowCategory.Finance);

	// ReSharper disable once UnusedType.Global
	public class OtherIncomeAccountTests : RightSideTests<OtherIncomeAccount>;

	// ReSharper disable once UnusedType.Global
	public class OtherExpenseAccountTests
		: LeftSideTests<OtherExpenseAccount>;

	public abstract class AccountTestsBase<TAccount>(
		bool acceptsCash = false,
		params CashFlowCategory[] invalidCashFlowCategories)
		: ValueObjectTests<TAccount> where TAccount : Account {
		[AutoFixtureData]
		public void InvalidConfigurationsThrow(TAccount account) {
			if (invalidCashFlowCategories.Length == 0) {
				return;
			}
			Assert.Multiple(Array.ConvertAll<CashFlowCategory, Action>(invalidCashFlowCategories, cashFlowCategory =>
				() => Assert.Throws<AccountExpectedCashFlowCategoryException>(() =>
					Account.For(account.AccountNumber, account.AccountName, cashFlowCategory, acceptsCash))));
		}

		[AutoFixtureData]
		public void ValidConfigurationsDoNotThrow(TAccount account) {
			var cashFlowCategories = CashFlowCategory.All.Except(invalidCashFlowCategories).ToArray();
			if (cashFlowCategories.Length == 0) {
				return;
			}

			Assert.Multiple(Array.ConvertAll<CashFlowCategory, Action>(
				cashFlowCategories, cashFlowCategory =>
					() => _ = Account.For(account.AccountNumber, account.AccountName, cashFlowCategory, acceptsCash)));
		}
	}

	public abstract class LeftSideTests<TAccount>(
		bool acceptsCash = false,
		params CashFlowCategory[] invalidCashFlowCategories)
		: AccountTestsBase<TAccount>(acceptsCash, invalidCashFlowCategories) where TAccount : Account {
		[AutoFixtureData]
		public void DebitsIncreaseBalance(TAccount sut, Money amount) =>
			Assert.Equal(amount, sut.Debit(amount).Balance);

		[AutoFixtureData]
		public void CreditsDecreaseBalance(TAccount sut, Money amount) =>
			Assert.Equal(-amount, sut.Credit(amount).Balance);
	}

	public abstract class RightSideTests<TAccount>(
		bool acceptsCash = false,
		params CashFlowCategory[] invalidCashFlowCategories)
		: AccountTestsBase<TAccount>(acceptsCash, invalidCashFlowCategories) where TAccount : Account {
		[AutoFixtureData]
		public void CreditsIncreaseBalance(TAccount sut, Money amount) =>
			Assert.Equal(amount, sut.Credit(amount).Balance);
		
		[AutoFixtureData]
		public void DebitsDecreaseBalance(TAccount sut, Money amount) =>
			Assert.Equal(-amount, sut.Debit(amount).Balance);
	}
}
