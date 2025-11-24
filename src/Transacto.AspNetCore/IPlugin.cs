namespace Transacto;

public interface IPluginDescriptor {
	public string Name { get; }
	IEnumerable<Type> MessageTypes { get; }
	
}
public interface IPlugin : IPluginDescriptor {
	public void Configure(IEndpointRouteBuilder builder) { }
	public void ConfigureServices(IServiceCollection services) { }
}
