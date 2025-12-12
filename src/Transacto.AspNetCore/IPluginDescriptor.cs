using Inflector;

namespace Transacto;

public interface IPluginDescriptor {
	public string Name { get; }
	public string DisplayName =>  Name.Underscore().Humanize();
	public string Slug => Name.Underscore().Dasherize();
	IEnumerable<Type> MessageTypes { get; }
	
	IEnumerable<LinkRelation> LinkRelations { get; }
}
