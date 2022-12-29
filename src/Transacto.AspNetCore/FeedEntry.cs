using System.Collections.Immutable;

namespace Transacto;

public interface IFeedEntry {
	public ImmutableArray<string> Events { get; init; }
}
