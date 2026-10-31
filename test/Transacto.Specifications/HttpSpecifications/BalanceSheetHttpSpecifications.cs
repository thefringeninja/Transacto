using NodaTime;
using NodaTime.Text;
using Transacto.Domain;
using Transacto.Framework.Http.Html;
using Transacto.Messages;
using Transacto.Plugins.FinancialStatements;
using Transacto.Plugins.FinancialStatements.BalanceSheet;
using Transacto.Testing.Http;

namespace Transacto.HttpSpecifications;

public class BalanceSheetHttpSpecifications : HttpSpecifications {
	private static SiteRoot SiteRoot { get; } = new(new UriBuilder { Path = "/financial-statements" }.Uri);
	private static Formatter GetSettings() => Formatter.Default;

	public class OpeningTheBooks(LocalDate openedOn) : HttpSpecification(
		new HttpScenario()
			.When(HttpRequest.Command(new OpenGeneralLedger {
				OpenedOn = openedOn
			}, "/general-ledger"))
			.Then(HttpRequest.GetHalJson(
					$"/financial-statements/balance-sheet?thru={LocalDatePattern.Iso.Format(openedOn)}"),
				HttpResponse.HalJson(new BalanceSheetReports([
						new ReportGroup(
							openedOn,
							new Report.Assets([]),
							new Report.Liabilities([]),
							new Report.Equity([]),
							decimal.Zero)
					]),
					new BalanceSheetReportsRepresentation(SiteRoot, GetSettings))));

	public class WhenAnEntryIsPosted(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
		LocalDate createdOn,
		Money subtotal,
		Money shipping,
		Money tax,
		Money inventory) : HttpSpecification(
		new HttpScenario()
			.When(DefineChartOfAccounts)
			.When(OpenBooks(createdOn))
			.When(HttpRequest.Command(new PostGeneralLedgerEntry {
				GeneralLedgerEntryId = generalLedgerEntryIdentifier.ToGuid(),
				CreatedOn = createdOn,
				BusinessTransaction = new Invoice(subtotal, shipping, tax, inventory)
					.ToJournalEntry(ChartOfAccounts.Factory()),
				Period = AccountingPeriod.Open(createdOn).ToString()
			}, "/general-ledger/entries"))
			.Then(HttpRequest.GetHalJson(
					$"/financial-statements/balance-sheet?thru={LocalDatePattern.Iso.Format(createdOn.PlusDays(1))}"),
				HttpResponse.HalJson(new BalanceSheetReports([
						new ReportGroup(
							createdOn.PlusDays(1),
							new Report.Assets([
								new Report.Line(new AccountInfo(1100, "Cash on Hand"),
									(subtotal + tax + shipping).ToDecimal()),
								new Report.Line(new AccountInfo(1220, "Raw Materials Inventory"),
									-inventory.ToDecimal())
							]),
							new Report.Liabilities([
								new Report.Line(new AccountInfo(2300, "Accrued Expenses"), tax.ToDecimal())
							]),
							new Report.Equity([
								new Report.Line(new AccountInfo(4000, "Sales Income"), subtotal.ToDecimal()),
								new Report.Line(new AccountInfo(4050, "Shipping and Handling Income"),
									shipping.ToDecimal()),
								new Report.Line(new AccountInfo(5000, "Cost of Goods Sold, Inventory"),
									inventory.ToDecimal())
							]),
							decimal.Zero)
					]),
					new BalanceSheetReportsRepresentation(SiteRoot, GetSettings))));

	private class Invoice(Money subtotal, Money shipping, Money tax, Money inventory)
		: IBusinessTransaction {
		public GeneralLedgerEntrySequenceNumber? SequenceNumber { get; set; }
		public static GeneralLedgerEntryNumberPrefix Prefix { get; } = new("invoice");

		public GeneralLedgerEntryNumber? ReferenceNumber =>
			SequenceNumber.HasValue ? new GeneralLedgerEntryNumber(Prefix, SequenceNumber.Value) : null;

		public IEnumerable<object> GetTransactionItems(ChartOfAccounts chartOfAccounts) {
			var total = subtotal + shipping + tax;
			yield return new Debit(new AccountNumber(1100), total);
			yield return new Credit(new AccountNumber(1220), inventory);
			yield return new Credit(new AccountNumber(2300), tax);
			yield return new Credit(new AccountNumber(4000), subtotal);
			yield return new Credit(new AccountNumber(4050), shipping);
			yield return new Debit(new AccountNumber(5000), inventory);
		}
	}
}
