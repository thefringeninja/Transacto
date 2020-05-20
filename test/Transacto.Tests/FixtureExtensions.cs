using System;
using AutoFixture;
using Transacto.Domain;

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
			fixture.Customize<Period>(composer => composer.FromFactory<DateTimeOffset>(Period.Open));

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
	}
}
