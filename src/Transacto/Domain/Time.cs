using NodaTime;
using NodaTime.Text;

namespace Transacto.Domain {
	public static class Time {
		private static LocalDatePattern LocalDatePattern = LocalDatePattern.CreateWithInvariantCulture("yyyyMMdd");
		private static LocalDateTimePattern LocalDateTimePattern = LocalDateTimePattern.ExtendedIso;

		public static class Parse {
			public static LocalDate LocalDate(string value) => LocalDatePattern.Parse(value).Value;

			public static LocalDateTime LocalDateTime(string value) => LocalDateTimePattern.Parse(value).Value;
		}

		public static class Format {
			public static string LocalDate(LocalDate value) => LocalDatePattern.Format(value);
			public static string LocalDateTime(LocalDateTime value) => LocalDateTimePattern.Format(value);
		}
	}
}
