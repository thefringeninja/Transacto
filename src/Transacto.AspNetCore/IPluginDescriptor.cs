using System.Collections.ObjectModel;
using Inflector;

namespace Transacto;

public interface IPluginDescriptor {
	public string Name { get; }
	public string DisplayName =>  Name.Underscore().Humanize();
	public string Slug => Name.Underscore().Dasherize();
	IEnumerable<Type> MessageTypes { get; }

	// Events another plugin owns that this one reads into its own type, keyed by storage name. Lets a plugin follow
	// an event without referencing the plugin that registers it, which the shared message type map would reject.
	IReadOnlyDictionary<string, Type> ConsumedMessageTypes => ReadOnlyDictionary<string, Type>.Empty;
	
	IEnumerable<LinkRelation> LinkRelations { get; }
}
