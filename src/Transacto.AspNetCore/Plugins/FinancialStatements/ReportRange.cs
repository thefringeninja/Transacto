using NodaTime;
using NodaTime.Text;

namespace Transacto.Plugins.FinancialStatements;

public record ReportRange(LocalDate StartingOn, Period Period) {
	public override string ToString() =>
		$"{LocalDatePattern.Iso.Format(StartingOn)} - {LocalDatePattern.Iso.Format(StartingOn.Plus(Period))}";

	public string ToFormUrlEncoded() =>
		$"starting-on={LocalDatePattern.Iso.Format(StartingOn)}&period={PeriodPattern.NormalizingIso.Format(Period)}";

	public static string ToFormUrlEncoded(IEnumerable<ReportRange> values) =>
		string.Join('&', values.Select(value => value.ToFormUrlEncoded()));
}

public interface ISomething {
	public static abstract bool IsMatch(Hallo.Link link);
}
