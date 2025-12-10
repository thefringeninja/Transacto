using Hallo;
using Microsoft.AspNetCore.Html;

namespace Transacto.Framework.Http.Html;

public class ViewContext {
	public Link[] _links { get; set; } = [];
	public object? _embedded { get; set; }

	public Func<Link, Type> GetTemplatedLink { get; set; } = _ => null!;
}
