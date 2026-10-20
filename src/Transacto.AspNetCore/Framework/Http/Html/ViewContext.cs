using Hallo;

namespace Transacto.Framework.Http.Html;

public class ViewContext(LinkRelationCollection linkRelations) {
	public LinkRelationCollection LinkRelations { get; } = linkRelations;
	public Link[] _links { get; set; } = [];
	public object? _embedded { get; set; }

	public Func<Link, Type> GetTemplatedLink { get; set; } = _ => null!;
	
	public string? Title { get; set; }
}
