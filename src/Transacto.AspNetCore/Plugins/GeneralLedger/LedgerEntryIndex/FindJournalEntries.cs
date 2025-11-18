using System.Collections.Immutable;
using Hallo;

namespace Transacto.Plugins.GeneralLedger.LedgerEntryIndex;

public delegate Task<ImmutableArray<SearchResult>> SearchJournalEntriesAsync(string prefix,
	CancellationToken ct);

public record SearchResult(string ReferenceNumber, Guid GeneralLedgerEntryId, DateTime CreatedOn);

public class SearchResultRepresentation : Hal<ImmutableArray<SearchResult>>, IHalState<ImmutableArray<SearchResult>>,
	IHalLinks<ImmutableArray<SearchResult>> {
	public object StateFor(ImmutableArray<SearchResult> results) => new { results };
	public IEnumerable<Link> LinksFor(ImmutableArray<SearchResult> resource) {
		foreach (var result in resource) {
			yield return new Link("t:ledger", $"../{result.GeneralLedgerEntryId}", title: result.ReferenceNumber);
		}
	}
}
