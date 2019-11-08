using System.Globalization;

namespace SomeCompany.BalanceSheet {
    internal static class Schema {
        internal static readonly Inflector.Inflector Inflector = new Inflector.Inflector(CultureInfo.GetCultureInfo("en-US"));

        public static class BalanceSheetReport {
            public static readonly string Table = Inflector.Underscore(nameof(BalanceSheetReport));

            public static class Columns {
                public static readonly string GeneralLedgerEntryId = Inflector.Underscore(nameof(GeneralLedgerEntryId));
                public static readonly string Balance = Inflector.Underscore(nameof(Balance));
                public static readonly string PeriodMonth = Inflector.Underscore(nameof(PeriodMonth));
                public static readonly string PeriodYear = Inflector.Underscore(nameof(PeriodYear));
                public static readonly string AccountNumber = Inflector.Underscore(nameof(AccountNumber));
            }
        }
    }
}
