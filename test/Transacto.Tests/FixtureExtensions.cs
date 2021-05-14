using System;
using System.Threading;
using AutoFixture;
using AutoFixture.Kernel;
using NodaTime;
using Transacto.Domain;

namespace Transacto {
	public static class FixtureExtensions {
		public static void CustomizeAccountName(this IFixture fixture) =>
			fixture.Customize<AccountName>(composer =>
				composer.FromFactory<Random>(r => new AccountName(new string('a', r.Next(1, AccountName.MaxLength)))));

		public static void CustomizeAccountNumber(this IFixture fixture) =>
			fixture.Customize<AccountNumber>(composer =>
				composer.FromFactory<Random>(r => new AccountNumber(r.Next(1000, 8999))));

		public static void CustomizeAccount(this IFixture fixture) {
			fixture.CustomizeAccount<AssetAccount>(1000);
			fixture.CustomizeAccount<LiabilityAccount>(2000);
			fixture.CustomizeAccount<EquityAccount>(3000);
			fixture.CustomizeAccount<IncomeAccount>(4000);
			fixture.CustomizeAccount<ExpenseAccount>(5000);
		}

		private static void CustomizeAccount<TAccount>(this IFixture fixture, int lowestAccountNumber)
			where TAccount : Account => fixture.Customize<TAccount>(composer => composer.FromFactory<AccountName, int>(
			(name, value) => (TAccount)Account.For(name, new AccountNumber(lowestAccountNumber + value % 1000))));


		public static void CustomizeAccountingPeriod(this IFixture fixture) =>
			fixture.Customize<AccountingPeriod>(composer => composer.FromFactory<LocalDate>(AccountingPeriod.Open));

		public static void CustomizeMoney(this IFixture fixture) =>
			fixture.Customize<Money>(composer =>
				composer.FromFactory<Random>(r => new Money(Math.Abs(Convert.ToDecimal(r.Next(1, 10000) / 100)))));

		public static void CustomizeCredit(this IFixture fixture) =>
			fixture.Customize<Credit>(composer =>
				composer.FromFactory<AccountNumber, Money>((n, m) => new Credit(n, m)));

		public static void CustomizeDebit(this IFixture fixture) =>
			fixture.Customize<Debit>(composer =>
				composer.FromFactory<AccountNumber, Money>((n, m) => new Debit(n, m)));

		public static void CustomizeGeneralLedgerEntryNumber(this IFixture fixture) =>
			fixture.Customize<GeneralLedgerEntryNumber>(composer =>
				composer.FromFactory<Random, int>((r, i) =>
					new GeneralLedgerEntryNumber(
						new string('a', r.Next(1, GeneralLedgerEntryNumber.MaxPrefixLength)), i)));

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
			public object Create(object request, ISpecimenContext context) {
				if (!typeof(DateTimeOffset).Equals(request)) {
					return new NoSpecimen();
				}

				return _seed.AddMonths(Interlocked.Increment(ref _baseValue));
			}
		}
	}
}
