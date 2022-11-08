using AutoFixture;
using AutoFixture.Kernel;
using NodaTime;
using Transacto.Domain;

namespace Transacto; 

public static class FixtureExtensions {
	public static void CustomizeAccountName(this IFixture fixture) {
		int number = 0;
		fixture.Customize<AccountName>(composer =>
			composer.FromFactory(() => new AccountName(new string('a', number++ % AccountName.MaxLength + 1))));
	}

	public static void CustomizeAccountNumber(this IFixture fixture) {
		int number = 0;
		fixture.Customize<AccountNumber>(composer =>
			composer.FromFactory(() => new AccountNumber(number++ % 7000 + 1000)));
	}

	public static void CustomizeAccount(this IFixture fixture) {
		fixture.CustomizeAccount<AssetAccount>(1000);
		fixture.CustomizeAccount<LiabilityAccount>(2000);
		fixture.CustomizeAccount<EquityAccount>(3000);
		fixture.CustomizeAccount<IncomeAccount>(4000);
		fixture.CustomizeAccount<ExpenseAccount>(5000);
	}

	private static void CustomizeAccount<TAccount>(this IFixture fixture, int lowestAccountNumber)
		where TAccount : Account => fixture.Customize<TAccount>(composer => composer.FromFactory<AccountName, int>(
		(name, value) => (TAccount)Account.For(new AccountNumber(lowestAccountNumber + value % 1000), name)));


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

	public static void CustomizeGeneralLedgerEntryNumberPrefix(this IFixture fixture) {
		int number = 0;
		fixture.Customize<GeneralLedgerEntryNumberPrefix>(composer =>
			composer.FromFactory(() => new GeneralLedgerEntryNumberPrefix(new string('a', number++ % 5 + 1))));
	}

	public static void CustomizeNodaTime(this IFixture fixture) {
		fixture.Customize<DateTimeOffset>(_ => new IncreasingDateTimeOffsetGenerator(DateTimeOffset.Now));
		fixture.Customize<OffsetDateTime>(composer =>
			composer.FromFactory<DateTimeOffset>(OffsetDateTime.FromDateTimeOffset));
		fixture.Customize<LocalDateTime>(composer => composer.FromFactory<OffsetDateTime>(x => x.LocalDateTime));
		fixture.Customize<LocalDate>(composer => composer.FromFactory<OffsetDateTime>(x => x.Date));
		fixture.Customize<YearMonth>(composer => composer.FromFactory<LocalDate>(x => x.ToYearMonth()));
	}

	private class IncreasingDateTimeOffsetGenerator : ISpecimenBuilder {
		private readonly DateTimeOffset _seed;
		private int _baseValue;

		public IncreasingDateTimeOffsetGenerator(DateTimeOffset seed) {
			_seed = seed;
			_baseValue = 0;
		}

		public object Create(object request, ISpecimenContext context) =>
			typeof(DateTimeOffset) != request?.GetType()
				? new NoSpecimen()
				: _seed.AddMonths(Interlocked.Increment(ref _baseValue));
	}
}
