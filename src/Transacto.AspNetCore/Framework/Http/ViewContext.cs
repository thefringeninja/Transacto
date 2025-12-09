using Hallo;

namespace Transacto.Framework.Http;

public class ViewContext {
	public Link[] _links { get; set; } = [];
	public object? _embedded { get; set; }
}
