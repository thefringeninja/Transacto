using System;
using System.Threading;
using AutoFixture;
using AutoFixture.Kernel;
using NodaTime;
using Transacto.Domain;
using Period = Transacto.Domain.Period;

namespace Transacto {
	public static class FixtureExtensions {
		public static void CustomizeAccountName(this IFixture fixture) =>
			fixture.Customize<AccountName>(composer =>
				composer.FromFactory<Random>(r => new AccountName(new string('a', r.Next(1, AccountName.MaxLength)))));

		public static void CustomizeAccountNumber(this IFixture fixture) =>
			fixture.Customize<AccountNumber>(composer =>
				composer.FromFactory<Random>(r => new AccountNumber(r.Next(1000, 8999))));

		public static void CustomizeAccountType(this IFixture fixture) =>
			fixture.Customize<AccountType>(composer =>
				composer.FromFactory<Random>(r => AccountType.All[r.Next(0, AccountType.All.Count)]));

		public static void CustomizePeriodIdentifier(this IFixture fixture) =>
			fixture.Customize<Period>(composer => composer.FromFactory<LocalDate>(Period.Open));

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
