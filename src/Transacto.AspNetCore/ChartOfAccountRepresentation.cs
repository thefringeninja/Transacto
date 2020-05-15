using System.Collections.Generic;
using Hallo;

namespace Transacto {
	internal class ChartOfAccountRepresentation : Hal<SortedDictionary<string, string>>,
		IHalLinks<SortedDictionary<string, string>>,
		IHalState<SortedDictionary<string, string>> {
		public IEnumerable<Link> LinksFor(SortedDictionary<string, string> resource) {
			yield break;
		}

		public object StateFor(SortedDictionary<string, string> resource) => resource;
	}
}
