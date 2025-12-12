namespace Transacto;

public interface IPlugin : IPluginDescriptor {
	public void Configure(IEndpointRouteBuilder builder) { }
	public void ConfigureServices(IServiceCollection services) { }
}
