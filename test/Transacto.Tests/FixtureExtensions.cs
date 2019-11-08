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

        public static void CustomizePeriodIdentifier(this IFixture fixture) =>
            fixture.Customize<PeriodIdentifier>(composer =>
                composer.FromFactory<Random>(r => new PeriodIdentifier(r.Next(1, 12), r.Next(0, 9999))));

        public static void CustomizeMoney(this IFixture fixture) =>
            fixture.Customize<Money>(composer =>
                composer.FromFactory<Random>(r => new Money(Math.Abs(Convert.ToDecimal(r.Next(1, 10000) / 100)))));
    }
}
