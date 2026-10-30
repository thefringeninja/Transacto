using System.Collections.ObjectModel;
using Hallo;

namespace Transacto;

public record LinkRelation(string Rel, string Title) {
	public Link To(string href, string? type = null) => new(Rel, href, type, title: Title);
}

public class LinkRelationCollection : KeyedCollection<string, LinkRelation> {
	protected override string GetKeyForItem(LinkRelation item) => item.Rel;
}
