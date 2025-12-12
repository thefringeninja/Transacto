using NodaTime.Text;

namespace Transacto.Framework.Http.Html;

public record FormatSettings(
	LocalDatePattern ShortDatePattern,
	LocalDatePattern LongDatePattern) {
	public static readonly FormatSettings Default = new(LocalDatePattern.Iso, LocalDatePattern.CreateWithCurrentCulture("D"));
}
