using Inflector;
using Transacto.Domain;
using Transacto.Framework.Projections;
using Transacto.Infrastructure.SQLite;
using Transacto.Messages;

namespace Transacto.Plugins.BalanceSheet;

public class BalanceSheetProjection : SQLiteProjection {
	public BalanceSheetProjection() : base(new Scripts()) {
		When<CreateSchema>();
		When<AccountDefined>(e => [
			Sql.Int32(e.AccountNumber).ToDbParameter(nameof(e.AccountNumber).Underscore()),
			Sql.String(e.AccountName).ToDbParameter(nameof(e.AccountName).Underscore())
		]);
		When<AccountRenamed>(e => [
			Sql.Int32(e.AccountNumber).ToDbParameter(nameof(e.AccountNumber).Underscore()),
			Sql.String(e.NewAccountName).ToDbParameter(nameof(e.NewAccountName).Underscore())
		]);
		When<GeneralLedgerEntryCreated>(e => {
			var date = Time.Parse.LocalDateTime(e.CreatedOn).Date.ToDateTimeUnspecified();

			return [
				Sql.Date(date).ToDbParameter(nameof(date).Underscore()),
				Sql.Guid(e.GeneralLedgerEntryId).ToDbParameter(nameof(e.GeneralLedgerEntryId).Underscore())
			];
		});
		When<CreditApplied>(e => {
			var amount = Account.For(new AccountNumber(e.AccountNumber))
				.Credit(new Money(e.Amount)).Balance
				.ToDecimal();

			return [
				Sql.Guid(e.GeneralLedgerEntryId).ToDbParameter(nameof(e.GeneralLedgerEntryId).Underscore()),
				Sql.Int32(e.AccountNumber).ToDbParameter(nameof(e.AccountNumber).Underscore()),
				Sql.Int64((int)(amount * 100)).ToDbParameter(nameof(amount).Underscore())
			];
		});
		When<DebitApplied>(e => {
			var amount = Account.For(new AccountNumber(e.AccountNumber))
				.Debit(new Money(e.Amount)).Balance
				.ToDecimal();

			return [
				Sql.Guid(e.GeneralLedgerEntryId).ToDbParameter(nameof(e.GeneralLedgerEntryId).Underscore()),
				Sql.Int32(e.AccountNumber).ToDbParameter(nameof(e.AccountNumber).Underscore()),
				Sql.Int64((int)(amount * 100)).ToDbParameter(nameof(amount).Underscore())
			];
		});
		When<GeneralLedgerEntryPosted>(e => [
			Sql.Guid(e.GeneralLedgerEntryId).ToDbParameter(nameof(e.GeneralLedgerEntryId).Underscore())
		]);
	}
}
