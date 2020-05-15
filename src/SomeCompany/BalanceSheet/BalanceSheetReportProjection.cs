using System;
using Transacto;
using Transacto.Messages;

namespace SomeCompany.BalanceSheet {
    public class BalanceSheetReportProjection : NpgsqlProjectionBase {
        public BalanceSheetReportProjection(string schema) : base(new Scripts()) {
            When<CreateSchema>();

            When<GeneralLedgerEntryCreated>(e => new[] {
                Sql.UniqueIdentifier(e.GeneralLedgerEntryId).ToDbParameter("general_ledger_entry_id"),
                Sql.Int(e.Period.Month).ToDbParameter("period_month"),
                Sql.Int(e.Period.Year).ToDbParameter("period_year")
            });

            When<CreditApplied>(e => new [] {
                Sql.UniqueIdentifier(e.GeneralLedgerEntryId).ToDbParameter("general_ledger_entry_id"),
                Sql.Int(e.AccountNumber).ToDbParameter("account_number"),
                Sql.Money(e.Amount).ToDbParameter("credit")
            });

            When<DebitApplied>(e => new [] {
                Sql.UniqueIdentifier(e.GeneralLedgerEntryId).ToDbParameter("general_ledger_entry_id"),
                Sql.Int(e.AccountNumber).ToDbParameter("account_number"),
                Sql.Money(e.Amount).ToDbParameter("debit")
            });

            When<GeneralLedgerEntryPosted>(e => new[] {
                Sql.UniqueIdentifier(e.GeneralLedgerEntryId).ToDbParameter("general_ledger_entry_id")
            });
        }

        public class Item {
            public int PeriodMonth { get; set; }
            public int PeriodYear { get; set; }
            public decimal Balance { get; set; }
            public int AccountNumber { get; set; }
        }

        public class GeneralLedgerEntryPeriod {
            public Guid GeneralLedgerEntryId { get; set; }
            public int PeriodMonth { get; set; }
            public int PeriodYear { get; set; }
        }

        public class UnpostedGeneralLedgerEntryLine {
            public Guid GeneralLedgerEntryId { get; set; }
            public decimal Credit { get; set; }
            public decimal Debit { get; set; }
            public int AccountNumber { get; set; }
        }
    }
}
