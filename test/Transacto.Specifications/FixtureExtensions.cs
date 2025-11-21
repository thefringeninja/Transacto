using AutoFixture;
using AutoFixture.Kernel;
using NodaTime;
using Transacto.Domain;

namespace Transacto;

internal static class FixtureExtensions {
	public static void CustomizeAccountName(this IFixture fixture) => fixture.Customize<AccountName>(composer =>
		composer.FromFactory<int>(number => new AccountName(new string('a', number % AccountName.MaxLength + 1))));

	public static void CustomizeAccountNumber(this IFixture fixture) => fixture.Customize<AccountNumber>(composer =>
		composer.FromFactory<int>(number => new AccountNumber(number % 7000 + 1000)));

	public static void CustomizeCashFlowCategory(this IFixture fixture) =>
		fixture.Customize<CashFlowCategory>(composer =>
			composer.FromFactory<int>(number => CashFlowCategory.All[number % CashFlowCategory.All.Length]));

	public static void CustomizeAccount(this IFixture fixture) {
		fixture.CustomizeAssetAccount(1000);
		fixture.CustomizeAccount<LiabilityAccount>(2000, CashFlowCategory.Operations, CashFlowCategory.Finance);
		fixture.CustomizeAccount<EquityAccount>(3000, CashFlowCategory.Finance);
		fixture.CustomizeAccount<IncomeAccount>(4000, CashFlowCategory.Operations);
		fixture.CustomizeAccount<CostOfRevenueExpenseAccount>(5000, CashFlowCategory.Operations);
		fixture.CustomizeAccount<OperatingExpenseAccount>(6000, CashFlowCategory.Operations);
		fixture.CustomizeAccount<OtherIncomeAccount>(7000, CashFlowCategory.Operations, CashFlowCategory.Investment,
			CashFlowCategory.Finance);
		fixture.CustomizeAccount<OtherExpenseAccount>(8000, CashFlowCategory.Operations, CashFlowCategory.Investment,
			CashFlowCategory.Finance);
		fixture.Customizations.Add(new TypeRelay(typeof(ExpenseAccount), typeof(CostOfRevenueExpenseAccount)));
	}

	private static void CustomizeAssetAccount(this IFixture fixture, int lowestAccountNumber) =>
		fixture.Customize<AssetAccount>(composer =>
			composer.FromFactory<AccountName, int, bool, bool>((name, number, shortTerm, cash) => {
				var accountNumber = new AccountNumber(lowestAccountNumber + number % 1000);
				return (shortTerm, cash) switch {
					(true, true) => (AssetAccount)Account.For(accountNumber, name, CashFlowCategory.Operations, true), // bank account
					(true, false) => (AssetAccount)Account.For(accountNumber, name, CashFlowCategory.Operations), // inventory, AR
					_ => (AssetAccount)Account.For(accountNumber, name, CashFlowCategory.Investment) // long term asset
				};
			}));

	private static void CustomizeAccount<TAccount>(this IFixture fixture, int lowestAccountNumber,
		CashFlowCategory cashFlowCategory, params CashFlowCategory[] cashFlowCategories)
		where TAccount : Account => fixture.Customize<TAccount>(composer =>
		composer.FromFactory<AccountName, int, int>((name, number, category) =>
			(TAccount)Account.For(
				new AccountNumber(lowestAccountNumber + number % 1000),
				name,
				((CashFlowCategory[])[cashFlowCategory, ..cashFlowCategories])
				[category % (cashFlowCategories.Length + 1)]
			)));

	public static void CustomizeAccountingPeriod(this IFixture fixture) =>
		fixture.Customize<AccountingPeriod>(composer => composer.FromFactory<LocalDate>(AccountingPeriod.Open));

	public static void CustomizeMoney(this IFixture fixture) =>
		fixture.Customize<Money>(composer => composer.FromFactory<decimal>(d => new Money(d)));

	public static void CustomizeCredit(this IFixture fixture) =>
		fixture.Customize<Credit>(composer =>
			composer.FromFactory<AccountNumber, Money>((n, m) => new Credit(n, m)));

	public static void CustomizeDebit(this IFixture fixture) =>
		fixture.Customize<Debit>(composer =>
			composer.FromFactory<AccountNumber, Money>((n, m) => new Debit(n, m)));

	public static void CustomizeGeneralLedgerEntryNumberPrefix(this IFixture fixture) =>
		fixture.Customize<GeneralLedgerEntryNumberPrefix>(composer =>
			composer.FromFactory<int>(number => new GeneralLedgerEntryNumberPrefix(new string('a', number % 5 + 1))));

	public static void CustomizeNodaTime(this IFixture fixture) {
		fixture.Customize<DateTimeOffset>(_ => new IncreasingDateTimeOffsetGenerator(DateTimeOffset.Now));
		fixture.Customize<OffsetDateTime>(composer =>
			composer.FromFactory<DateTimeOffset>(OffsetDateTime.FromDateTimeOffset));
		fixture.Customize<LocalDateTime>(composer => composer.FromFactory<OffsetDateTime>(x => x.LocalDateTime));
		fixture.Customize<LocalDate>(composer => composer.FromFactory<OffsetDateTime>(x => x.Date));
		fixture.Customize<YearMonth>(composer => composer.FromFactory<LocalDate>(x => x.ToYearMonth()));
	}

	private class IncreasingDateTimeOffsetGenerator(DateTimeOffset seed) : ISpecimenBuilder {
		private int _baseValue = 0;

		public object Create(object request, ISpecimenContext context) =>
			typeof(DateTimeOffset) != request?.GetType()
				? new NoSpecimen()
				: seed.AddMonths(Interlocked.Increment(ref _baseValue));
	}
}
