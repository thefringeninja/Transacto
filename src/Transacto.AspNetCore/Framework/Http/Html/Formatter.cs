using NodaTime.Text;

namespace Transacto.Framework.Http.Html;

public record Formatter(
	LocalDatePattern ShortDatePattern,
	LocalDatePattern LongDatePattern) {
	public static readonly Formatter Default = new(LocalDatePattern.Iso, LocalDatePattern.CreateWithCurrentCulture("D"));
}
