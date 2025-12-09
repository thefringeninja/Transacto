using System.Collections.Immutable;

namespace Transacto.Plugins;

public record Index(ImmutableArray<Index.Plugin> Plugins) {
	public record Plugin(string Name, string Slug);

	public static Index Create(IPlugin[] plugins) => new([
		..Array.ConvertAll(plugins, plugin => new Plugin(plugin.DisplayName, plugin.Slug))
	]);
}
