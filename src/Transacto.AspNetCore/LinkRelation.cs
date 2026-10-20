using System.Collections.ObjectModel;

namespace Transacto;

public record LinkRelation(string Rel, string Title);

public class LinkRelationCollection : KeyedCollection<string, LinkRelation> {
	protected override string GetKeyForItem(LinkRelation item) => item.Rel;
}
