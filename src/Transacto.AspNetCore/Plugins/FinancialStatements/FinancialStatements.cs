using Hallo;
using NodaTime;
using NodaTime.Text;
using Transacto.Framework;
using Transacto.Framework.Http;

namespace Transacto.Plugins.FinancialStatements;

public class FinancialStatements : IPlugin {
	public string Name => nameof(FinancialStatements);
	public IEnumerable<Type> MessageTypes { get; } = [];

	public void Configure(IEndpointRouteBuilder builder) {
		builder.MapGroup("/profit-and-loss").UseProfitAndLoss();
		builder.MapGroup("/balance-sheet").UseBalanceSheet();
		builder.MapGroup("/cash-flow").UseCashFlow();
		builder.MapGet("/", () => {
			var startOfYear = LocalDate.FromDateTime(DateTime.Now);

			return Results.Hal(
				new FinancialStatementsIndex(startOfYear),
				Checkpoint.None,
				new FinancialStatementsIndexRepresentation());
		});
	}

	public void ConfigureServices(IServiceCollection services) => services
		.AddProfitAndLoss()
		.AddBalanceSheet()
		.AddCashFlow();
}

public class FinancialStatementsIndexRepresentation : Hal<FinancialStatementsIndex>,
	IHalState<FinancialStatementsIndex>,
	IHalLinks<FinancialStatementsIndex> {
	public IEnumerable<Link> LinksFor(FinancialStatementsIndex resource) {
		yield return new Link("self", "financial-statements", title: "Index");
		yield return new Link("index", "financial-statements", title: "Index");

		var startOfYear = LocalDatePattern.Iso.Format(resource.StartOfYear);
		yield return new Link("t:financial-statement",
			$"financial-statements/balance-sheet?thru={startOfYear}", title: "Balance Sheet Report");
		yield return new Link("t:financial-statement",
			$"financial-statements/cash-flow?startingOn={startOfYear}&period=P1Y", title: "Cash Flow");
		yield return new Link("t:financial-statement",
			$"financial-statements/profit-and-loss?startingOn={startOfYear}&period=P1Y", title: "Profit and Loss");
	}

	public object StateFor(FinancialStatementsIndex resource) => resource;
}

public readonly record struct FinancialStatementsIndex(LocalDate StartOfYear) {
	public LocalDate EndOfYear => StartOfYear.PlusYears(1);
}
