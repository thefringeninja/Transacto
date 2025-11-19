using System.Collections.Immutable;
using Hallo;

namespace Transacto.Plugins.GeneralLedger.LedgerEntryIndex;

public delegate Task<SearchResults> SearchJournalEntriesAsync(string prefix,
	CancellationToken ct);

public record SearchResults(ImmutableArray<SearchResult> Results);
public record SearchResult(string ReferenceNumber, Guid GeneralLedgerEntryId, DateTime CreatedOn);

public class SearchResultRepresentation : Hal<SearchResults>, IHalState<SearchResults>,
	IHalLinks<SearchResults> {
	public object StateFor(SearchResults results) => results;
	public IEnumerable<Link> LinksFor(SearchResults resource) {
		foreach (var result in resource.Results) {
			yield return new Link("t:ledger", $"../{result.GeneralLedgerEntryId}", title: result.ReferenceNumber);
		}
	}
}
