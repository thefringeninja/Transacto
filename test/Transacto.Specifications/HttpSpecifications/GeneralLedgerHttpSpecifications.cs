using NodaTime;
using Transacto.Domain;
using Transacto.Framework.Http.Html;
using Transacto.Plugins.GeneralLedger;
using Transacto.Plugins.GeneralLedger.LedgerEntryIndex;
using Transacto.Testing.Http;

namespace Transacto.HttpSpecifications;

public class GeneralLedgerHttpSpecifications : HttpSpecifications {
	private static SiteRoot SiteRoot { get; } = new(new UriBuilder { Path = "/general-ledger" }.Uri);

	public class Period(LocalDate openedOn) : HttpSpecification(
		new HttpScenario()
			.Then(HttpRequest.GetHalJson("/general-ledger/period"),
				HttpResponse.NotFound())
			.When(OpenBooks(openedOn))
			.Wait(TimeSpan.FromSeconds(2))
			.Then(HttpRequest.GetHalJson("/general-ledger/period"),
				HttpResponse.HalJson(new CurrentPeriod(AccountingPeriod.Open(openedOn).ToString()),
					new CurrentPeriodRepresentation())));

	public class SearchEntries(GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers, LocalDate openedOn)
		: HttpSpecification(
			new HttpScenario()
				.When(DefineChartOfAccounts)
				.When(OpenBooks(openedOn))
				.When(Array.ConvertAll(generalLedgerEntryIdentifiers, identifier => PostGeneralLedgerEntry(
					identifier, openedOn, new BusinessTransaction()
				)))
				.Wait(TimeSpan.FromSeconds(2))
				.Then(HttpRequest.GetHalJson("/general-ledger/entries/search"),
					HttpResponse.HalJson(
						new SearchResults([
							.. generalLedgerEntryIdentifiers.Select((identifier, i) =>
								new SearchResult(
									new GeneralLedgerEntryNumber(JournalEntry.Prefix, new GeneralLedgerEntrySequenceNumber(i + 1))
										.ToString(),
									identifier.ToGuid(), openedOn))
						]), new SearchResultsRepresentation(SiteRoot))));

	private class BusinessTransaction : IBusinessTransaction {
		public GeneralLedgerEntrySequenceNumber? SequenceNumber { get; set; }
		public static GeneralLedgerEntryNumberPrefix Prefix { get; } = new("b");

		public GeneralLedgerEntryNumber? ReferenceNumber =>
			SequenceNumber.HasValue ? new GeneralLedgerEntryNumber(Prefix, SequenceNumber.Value) : null;

		public IEnumerable<TransactionItem> GetTransactionItems(ChartOfAccounts chartOfAccounts) {
			yield return new Debit(new AccountNumber(1000), new Money(10m));
			yield return new Credit(new AccountNumber(1200), new Money(10m));
		}
	}
}
