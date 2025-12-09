// ReSharper disable CheckNamespace

using Microsoft.AspNetCore.Mvc;
using NodaTime;
using Transacto.Framework;
using Transacto.Framework.Http;
using Transacto.Plugins;
using Transacto.Plugins.FinancialStatements.CashFlow;

namespace Microsoft.AspNetCore.Builder;
// ReSharper restore CheckNamespace

static partial class ApplicationBuilderExtensions {
	public static IEndpointRouteBuilder UseCashFlow(this IEndpointRouteBuilder builder) {
		builder.MapGet("",
			async ([FromQuery] ParseableLocalDate[] startingOn, [FromQuery] ParseablePeriod[] period,
				[FromServices] GetCashFlowReportsAsync getCashFlowReportsAsync, CancellationToken ct) => {
				var reportRanges = new (LocalDate, Period)[Math.Min(startingOn.Length, period.Length)];

				for (var i = 0; i < reportRanges.Length; i++) {
					reportRanges[i] = (startingOn[i], period[i]);
				}
				var resource = await getCashFlowReportsAsync([..reportRanges], ct);

				return Results.Hal(resource, Checkpoint.None, new CashFlowReportsRepresentation());
			});

		return builder;
	}
}
