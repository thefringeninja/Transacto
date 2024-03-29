namespace Transacto;

public interface IPlugin {
	public string Name { get; }
	public void Configure(IEndpointRouteBuilder builder) { }
	public void ConfigureServices(IServiceCollection services) { }
	IEnumerable<Type> MessageTypes { get; }
}
