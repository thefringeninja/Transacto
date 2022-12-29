namespace Transacto.Framework.Http;

public record struct DocumentRevision(Optional<int> Value) {
	public static ValueTask<DocumentRevision?> BindAsync(HttpContext context) =>
		new(context.Request.Headers.IfMatch.ToArray() switch {
			{ Length: 0 or > 1 } => new DocumentRevision(Optional<int>.Empty),
			[var etag] => int.TryParse(etag, out var expectedRevision)
				? new DocumentRevision(Optional<int>.Empty)
				: new(expectedRevision),
			_ => new DocumentRevision(Optional<int>.Empty)
		});
}
