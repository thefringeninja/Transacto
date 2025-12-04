using Inflector;

namespace Transacto;

public interface IPluginDescriptor {
	public string Name { get; }
	public string DisplayName =>  Name.Underscore().Humanize();
	public string Slug => Name.Underscore().Dasherize();
	IEnumerable<Type> MessageTypes { get; }
	
}
public interface IPlugin : IPluginDescriptor {
	public void Configure(IEndpointRouteBuilder builder) { }
	public void ConfigureServices(IServiceCollection services) { }
}
