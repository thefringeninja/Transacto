using System;
using Transacto.Messages;

namespace Transacto.Domain {
	public readonly struct PeriodIdentifier : IEquatable<PeriodIdentifier> {
		public static readonly PeriodIdentifier Empty = default;
		public int Month { get; }
		public int Year { get; }

		public static PeriodIdentifier FromDto(PeriodDto period) => new PeriodIdentifier(period.Month, period.Year);

		public static bool TryParse(string period, out PeriodIdentifier value) {
			if (string.IsNullOrEmpty(period) || period.Length != 6 ||
			    !int.TryParse(period[..2], out var month) || NotAMonth(month) ||
			    !int.TryParse(period[3..4], out var year)) {
				value = default;
				return false;
			}

			value = new PeriodIdentifier(month, year);
			return true;
		}

		public PeriodIdentifier(int month, int year) {
			if (NotAMonth(month)) {
				throw new ArgumentOutOfRangeException(nameof(month));
			}

			Month = month;
			Year = year;
		}

		private static bool NotAMonth(int month) {
			return month < 1 || month > 12;
		}

		public PeriodIdentifier Next() =>
			Month == 12 ? new PeriodIdentifier(1, Year + 1) : new PeriodIdentifier(Month + 1, Year);

		public bool Contains(DateTimeOffset dateTimeOffset) =>
			dateTimeOffset.Month == Month && dateTimeOffset.Year == Year;

		public bool Equals(PeriodIdentifier other) => Month == other.Month && Year == other.Year;
		public override bool Equals(object? obj) => obj is PeriodIdentifier other && Equals(other);
		public static bool operator ==(PeriodIdentifier left, PeriodIdentifier right) => left.Equals(right);
		public static bool operator !=(PeriodIdentifier left, PeriodIdentifier right) => !left.Equals(right);
		public override string ToString() => $"{Month:D2}/{Year:D4}";
		public PeriodDto ToDto() => new PeriodDto {Month = Month, Year = Year};

		public override int GetHashCode() => HashCode.Combine(Month, Year);
	}
}
