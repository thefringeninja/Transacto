namespace Transacto.Framework.Http;

public record struct DocumentRevision(Option<int> Value) {
	public static ValueTask<DocumentRevision?> BindAsync(HttpContext context) =>
		new(context.Request.Headers.IfMatch.ToArray() switch {
			{ Length: 0 or > 1 } => new DocumentRevision(new None()),
			[var etag] => int.TryParse(etag, out var expectedRevision)
				? new DocumentRevision(new None())
				: new DocumentRevision(new Some<int>(expectedRevision)),
			_ => new DocumentRevision(new None())
		});
}
