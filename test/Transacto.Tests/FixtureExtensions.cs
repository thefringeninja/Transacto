using System;
using AutoFixture;
using Transacto.Domain;

namespace Transacto {
    internal static class FixtureExtensions {
        public static IFixture Customize(this IFixture fixture) {
            fixture.Customize<PeriodIdentifier>(composer =>
                composer.FromFactory<Random>(r => new PeriodIdentifier(r.Next(1, 12), r.Next(0, 9999))));

            fixture.Customize<AccountName>(composer =>
                composer.FromFactory<Random>(r => new AccountName(new string('a', r.Next(1, AccountName.MaxLength)))));

            fixture.Customize<AccountNumber>(composer =>
                composer.FromFactory<Random>(r => new AccountNumber(r.Next(1000, 8999))));

            return fixture;
        }
    }
}
