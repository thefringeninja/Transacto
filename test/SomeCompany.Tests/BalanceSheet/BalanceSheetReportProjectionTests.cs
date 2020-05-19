using System;
using System.Linq;
using System.Threading.Tasks;
using SomeCompany.Infrastructure;
using Transacto;
using Transacto.Domain;
using Transacto.Messages;
using Transacto.Plugins.BalanceSheet;
using Xunit;

namespace SomeCompany.BalanceSheet {
    public class BalanceSheetReportQueryHandlerTests {
        private readonly string _schema;

        public BalanceSheetReportQueryHandlerTests() {
            _schema = $"dbo{Guid.NewGuid():n}";
        }
    }
    public class BalanceSheetReportProjectionTests {
        private readonly string _schema;

        public BalanceSheetReportProjectionTests() {
            _schema = $"dbo{Guid.NewGuid():n}";
        }

        [Theory, AutoSomeCompanyData]
        public Task when_a_general_ledger_entry_was_created(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
            Period period) => new NpgsqlProjectionScenario(new BalanceSheetReportProjection())
            .Given(new CreateSchema())
            .Given(new GeneralLedgerEntryCreated {
                GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
                Period = period.ToString()
            })
            .Then(_schema, "balance_sheet_general_ledger_entry_period", new BalanceSheetReportProjection.GeneralLedgerEntryPeriod {
                PeriodMonth = period.Month,
                PeriodYear = period.Year,
                GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid()
            })
            .Assert();

        [Theory, AutoSomeCompanyData]
        public Task when_a_credit_was_applied(
            GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
            Period period,
            Credit credit) => new NpgsqlProjectionScenario(new BalanceSheetReportProjection())
            .Given(new CreateSchema())
            .Given(new GeneralLedgerEntryCreated {
                GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
                Period = period.ToString()
            }, new CreditApplied {
                Amount = credit.Amount.ToDecimal(),
                AccountNumber = credit.AccountNumber.ToInt32(),
                GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid()
            })
            .Then(_schema, "balance_sheet_general_ledger_entry_period", new BalanceSheetReportProjection.GeneralLedgerEntryPeriod {
                PeriodMonth = period.Month,
                PeriodYear = period.Year,
                GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid()
            })
            .Then(_schema, "balance_sheet_items_unposted", new BalanceSheetReportProjection.UnpostedGeneralLedgerEntryLine {
                AccountNumber = credit.AccountNumber.ToInt32(),
                GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
                Credit = credit.Amount.ToDecimal()
            })
            .Assert();

        [Theory, AutoSomeCompanyData]
        public Task when_a_debit_was_applied(
            GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
            Period period,
            Credit credit) => new NpgsqlProjectionScenario(new BalanceSheetReportProjection())
            .Given(new CreateSchema())
            .Given(new GeneralLedgerEntryCreated {
                GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
                Period = period.ToString()
            }, new DebitApplied {
                Amount = credit.Amount.ToDecimal(),
                AccountNumber = credit.AccountNumber.ToInt32(),
                GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid()
            })
            .Then(_schema, "balance_sheet_general_ledger_entry_period", new BalanceSheetReportProjection.GeneralLedgerEntryPeriod {
                PeriodMonth = period.Month,
                PeriodYear = period.Year,
                GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid()
            })
            .Then(_schema, "balance_sheet_items_unposted", new BalanceSheetReportProjection.UnpostedGeneralLedgerEntryLine {
                AccountNumber = credit.AccountNumber.ToInt32(),
                GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
                Debit = credit.Amount.ToDecimal()
            })
            .Assert();

        [Theory, AutoSomeCompanyData]
        public Task when_the_general_ledger_entry_was_posted(
            GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
            Period period,
            AccountNumber credit,
            AccountNumber debit,
            Money amount,
            int iterations) => new NpgsqlProjectionScenario(new BalanceSheetReportProjection())
            .Given(new CreateSchema())
            .Given(new GeneralLedgerEntryCreated {
                GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
                Period = period.ToString()
            })
            .Given(Enumerable.Range(0, iterations).Select(_ => new DebitApplied {
                Amount = amount.ToDecimal(),
                AccountNumber = debit.ToInt32(),
                GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid()
            }))
            .Given(Enumerable.Range(0, iterations).Select(_ => new CreditApplied {
                Amount = amount.ToDecimal(),
                AccountNumber = credit.ToInt32(),
                GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid()
            }))
            .Given(new GeneralLedgerEntryPosted {
                GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
                Period = period.ToString()
            })
            .Then(_schema, "balance_sheet_general_ledger_entry_period",
                Array.Empty<BalanceSheetReportProjection.GeneralLedgerEntryPeriod>())
            .Then(_schema, "balance_sheet_items_unposted",
                Array.Empty<BalanceSheetReportProjection.UnpostedGeneralLedgerEntryLine>())
            .Then(_schema, "balance_sheet_report", new BalanceSheetReportProjection.Item {
                Balance = amount.ToDecimal() * iterations,
                PeriodMonth = period.Month,
                PeriodYear = period.Year,
                AccountNumber = debit.ToInt32()
            }, new BalanceSheetReportProjection.Item {
                Balance = -amount.ToDecimal() * iterations,
                PeriodMonth = period.Month,
                PeriodYear = period.Year,
                AccountNumber = credit.ToInt32()
            })
            .Assert();
    }
}
