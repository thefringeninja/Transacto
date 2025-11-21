// ReSharper disable CheckNamespace

using Microsoft.AspNetCore.Mvc;
using NodaTime;
using Transacto.Framework;
using Transacto.Framework.Http;
using Transacto.Plugins.FinancialStatements.ProfitAndLoss;

namespace Microsoft.AspNetCore.Builder;
// ReSharper restore CheckNamespace

static partial class ApplicationBuilderExtensions {
	public static IEndpointRouteBuilder UseProfitAndLoss(this IEndpointRouteBuilder builder) {
		builder.MapGet("",
			async ([FromQuery] ParseableLocalDate[] startingOn, [FromQuery] ParseablePeriod[] period,
				[FromServices] GetProfitAndLossReportsAsync getProfitAndLossReportsAsync, CancellationToken ct) => {
				var reportRanges = new (LocalDate, Period)[Math.Min(startingOn.Length, period.Length)];

				for (var i = 0; i < reportRanges.Length; i++) {
					reportRanges[i] = (startingOn[i], period[i]);
				}

				var resource = await getProfitAndLossReportsAsync([..reportRanges], ct);

				return Results.Extensions.Hal(resource, Checkpoint.None, new ProfitAndLossReportsRepresentation());
			});
		return builder;
	}
}
