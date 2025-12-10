using Hallo;
using Transacto.Framework.Http.Html;

namespace Transacto.Plugins;

internal class IndexRepresentation(SiteRoot siteRoot) : Hal<Index>, IHalLinks<Index> {
	public IEnumerable<Link> LinksFor(Index resource) {
		yield return new Link("self", siteRoot.Uri.AbsolutePath);
		yield return new Link("index", siteRoot.Uri.AbsolutePath);
		foreach (var plugin in resource.Plugins) {
			yield return new Link("index", $"{siteRoot.Uri.AbsolutePath}{plugin.Slug}", title: plugin.Name);
		}
	}
}
