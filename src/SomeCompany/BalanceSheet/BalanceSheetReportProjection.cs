using System;
using Projac.Npgsql;
using Transacto;
using Transacto.Messages;

namespace SomeCompany.BalanceSheet {
	public class BalanceSheetReportProjection : NpgsqlProjection {
		public BalanceSheetReportProjection() : base(new Scripts()) {
			When<CreateSchema>();

			When<GeneralLedgerEntryCreated>(e => new[] {
				Sql.Parameter(() => e.GeneralLedgerEntryId),
				Sql.Int(e.Period.Month).ToDbParameter("period_month"),
				Sql.Int(e.Period.Year).ToDbParameter("period_year")
			});

			When<CreditApplied>(e => new[] {
				Sql.Parameter(() => e.GeneralLedgerEntryId),
				Sql.Parameter(() => e.AccountNumber),
				Sql.Money(e.Amount).ToDbParameter("credit")
			});

			When<DebitApplied>(e => new[] {
				Sql.Parameter(() => e.GeneralLedgerEntryId),
				Sql.Parameter(() => e.AccountNumber),
				Sql.Money(e.Amount).ToDbParameter("debit")
			});

			When<GeneralLedgerEntryPosted>(e => new[] {
				Sql.Parameter(() => e.GeneralLedgerEntryId),
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
