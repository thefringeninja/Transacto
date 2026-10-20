using Microsoft.Extensions.Internal;
using Microsoft.Net.Http.Headers;
using NodaTime;
using Transacto.Domain;
using Transacto.Plugins.GeneralLedger;

namespace Transacto.Plugins.FinancialStatements;

internal static class CachePolicies {
	private static readonly CacheControlHeaderValue s_ExpiresInFiveSeconds = new() {
		MaxAge = TimeSpan.FromSeconds(5),
		Private = true
	};

	private static readonly CacheControlHeaderValue s_ExpiresInOneYear = new() {
		MaxAge = TimeSpan.FromDays(365),
		Private = true
	};

	private static LocalDate Cutoff(AccountingPeriod accountingPeriod, Period period) =>
		period switch {
			{ Months: 1 } => accountingPeriod.LastDay,
			{ Months: 3 } => accountingPeriod.LastDayOfQuarter,
			{ Years: 1 } => new YearMonth(accountingPeriod.Year, 1).OnDayOfMonth(1),
			{ Days: > 0 } => accountingPeriod.LastDay,
			_ => LocalDate.MinIsoValue
		};

	public static RequestDelegate Thru(GetCurrentPeriodAsync getCurrentPeriodAsync) => async context => {
		var utcNow = context.RequestServices.GetRequiredService<ISystemClock>().UtcNow;

		var cacheControl = s_ExpiresInFiveSeconds;

		if (context.Response.StatusCode < 400) {
			var accountingPeriod = await getCurrentPeriodAsync(context.RequestAborted);
			if (!accountingPeriod.HasValue) {
				return;
			}

			var query = context.Request.Query;

			var count = query["thru"].Count;
			
			cacheControl = s_ExpiresInOneYear;
			
			for (var i = 0; i < count; i++) {
				if (!ParseableLocalDate.TryParse(query["thru"][i] ?? "", null, out var thruResult)) {
					break;
				}

				LocalDate thru = thruResult;

				var cutoff = Cutoff(accountingPeriod.Value, Period.FromDays(thru.DayOfYear));
				if (thru >= cutoff) {
					cacheControl = s_ExpiresInFiveSeconds;
					break;
				}
			}
		}

		var headers = context.Response.GetTypedHeaders();
		headers.CacheControl = cacheControl;
		headers.Expires = utcNow.Add(cacheControl.MaxAge!.Value);
	};

	public static RequestDelegate Range(GetCurrentPeriodAsync getCurrentPeriodAsync) => async (context) => {
		var utcNow = context.RequestServices.GetRequiredService<ISystemClock>().UtcNow;

		var cacheControl = s_ExpiresInFiveSeconds;

		if (context.Response.StatusCode < 400) {
			var accountingPeriod = await getCurrentPeriodAsync(context.RequestAborted);
			if (!accountingPeriod.HasValue) {
				return;
			}

			var query = context.Request.Query;
			var startingOnValues = query["starting-on"];
			var periodValues = query["period"];

			var count = Math.Min(startingOnValues.Count, periodValues.Count);

			cacheControl = s_ExpiresInOneYear;

			for (var i = 0; i < count; i++) {
				if (!ParseableLocalDate.TryParse(startingOnValues[i] ?? "", null, out var startingOnResult)) {
					break;
				}

				if (!ParseablePeriod.TryParse(periodValues[i] ?? "", null, out var periodResult)) {
					break;
				}

				LocalDate startingOn = startingOnResult;
				Period period = periodResult;

				var cutoff = Cutoff(accountingPeriod.Value, period);
				if (startingOn.Plus(period) >= cutoff) {
					cacheControl = s_ExpiresInFiveSeconds;
					break;
				}
			}
		}

		var headers = context.Response.GetTypedHeaders();
		headers.CacheControl = cacheControl;
		headers.Expires = utcNow.Add(cacheControl.MaxAge!.Value);
	};
}
