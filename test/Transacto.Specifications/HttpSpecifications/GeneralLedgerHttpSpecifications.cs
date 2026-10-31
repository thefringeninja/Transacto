using NodaTime;
using Transacto.Domain;
using Transacto.Framework.Http.Html;
using Transacto.Messages;
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
			.Then(HttpRequest.GetHalJson("/general-ledger/period"),
				HttpResponse.HalJson(new CurrentPeriod(openedOn.ToString()), new CurrentPeriodRepresentation())));

	public class SearchEntries(GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers, LocalDate openedOn)
		: HttpSpecification(
			new HttpScenario()
				.When(DefineChartOfAccounts)
				.When(OpenBooks(openedOn))
				.When(Array.ConvertAll(generalLedgerEntryIdentifiers, identifier => HttpRequest.Command(
					new PostGeneralLedgerEntry {
						GeneralLedgerEntryId = identifier.ToGuid(),
						CreatedOn = openedOn,
						Period = AccountingPeriod.Open(openedOn).ToString(),
						BusinessTransaction = new BusinessTransaction()
					}, "/general-ledger/entries")))
				.Then(HttpRequest.GetHalJson("/general-ledger/entries/search"),
					HttpResponse.HalJson(
						new SearchResults([
							.. generalLedgerEntryIdentifiers.Select((identifier, i) =>
								new SearchResult($"b-{i}", identifier.ToGuid(), openedOn))
						]), new SearchResultsRepresentation(SiteRoot))));

	private class BusinessTransaction : IBusinessTransaction {
		public GeneralLedgerEntrySequenceNumber? SequenceNumber { get; set; }
		public static GeneralLedgerEntryNumberPrefix Prefix { get; } = new("b");

		public GeneralLedgerEntryNumber? ReferenceNumber =>
			SequenceNumber.HasValue ? new GeneralLedgerEntryNumber(Prefix, SequenceNumber.Value) : null;

		public IEnumerable<object> GetTransactionItems(ChartOfAccounts chartOfAccounts) {
			yield return new Debit(new AccountNumber(1000), new Money(10m));
			yield return new Credit(new AccountNumber(1200), new Money(10m));
		}
	}
}
