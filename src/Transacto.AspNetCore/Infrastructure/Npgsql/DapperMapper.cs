using System.Data;
using Dapper;
using NodaTime;

namespace Transacto.Infrastructure.Npgsql;

public static class DapperMapper {
	public static void Initialize() {
		DefaultTypeMap.MatchNamesWithUnderscores = true;
		SqlMapper.AddTypeHandler(new LocalDateHandler());
	}

	private class LocalDateHandler : SqlMapper.TypeHandler<LocalDate> {
		public override void SetValue(IDbDataParameter parameter, LocalDate value) =>
			parameter.Value = value.AtMidnight().ToDateTimeUnspecified();

		public override LocalDate Parse(object value) =>
			value switch {
				null or DBNull => throw new DataException("Cannot convert null/DBNull to LocalDate"),
				LocalDate localDate => localDate,
				DateTime dateTime => LocalDateTime.FromDateTime(dateTime).Date,
				_ => throw new DataException($"Cannot convert {value.GetType()} to NodaTime.LocalDate")
			};
	}
}
