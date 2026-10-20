using NodaTime;
using NodaTime.Text;
using Transacto.Framework.Http.Html;
using Transacto.Messages;
using Transacto.Plugins.FinancialStatements.BalanceSheet;
using Transacto.Testing;

namespace Transacto.Http;

public class BalanceSheetHttpSpecifications {
	private static SiteRoot SiteRoot { get; } = new SiteRoot(new UriBuilder().Uri);
	private static Formatter GetSettings() => Formatter.Default;

	public class open_books(LocalDate openedOn) : EventSpecification(
		new Scenario()
			.Given("", HttpRequest.Command(new OpenGeneralLedger {
				OpenedOn = openedOn
			}, "/general-ledger", TransactoSerializerOptions.Messages))
			.When(HttpRequest.GetHalJson(
				$"/financial-statements/balance-sheet?thru={LocalDatePattern.Iso.Format(openedOn)}"))
			.Then("", HttpResponse.HalJson(new BalanceSheetReports([
					new ReportGroup(
						openedOn,
						new Report.Assets([]),
						new Report.Liabilities([]),
						new Report.Equity([]),
						decimal.Zero)
				]),
				new BalanceSheetReportsRepresentation(SiteRoot, GetSettings))));
}
