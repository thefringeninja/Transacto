using Hallo;
using Inflector;

namespace Transacto.Plugins;

internal class IndexRepresentation : Hal<Index>, IHalLinks<Index> {
	public IEnumerable<Link> LinksFor(Index resource) {
		yield return new Link("self", "./");
		yield return new Link("index", "./");
		foreach (var plugin in resource.Plugins) {
			yield return new Link("index", plugin.Slug, title: plugin.Name);
		}
	}
}
