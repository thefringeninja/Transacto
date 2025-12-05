using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Transacto.Domain;

namespace Transacto.Plugins.FinancialStatements.ProfitAndLoss;

public record ProfitAndLossReport(
	ReportRange ReportRange,
	Report.GrossRevenue GrossRevenue,
	Report.CostOfRevenue CostOfRevenue,
	Report.OperatingExpenses OperatingExpenses,
	Report.OtherExpenses OtherExpenses,
	Report.OtherIncome OtherIncome) {
	public decimal GrossProfit => GrossRevenue.Total - CostOfRevenue.Total;
	public decimal OperatingIncome => GrossProfit - OperatingExpenses.Total;
	public decimal NetProfit => OtherIncome.Total + OperatingIncome - OtherExpenses.Total;

	public static ProfitAndLossReport Create(ReportRange reportRange, ImmutableArray<Report.Line> lines) =>
		new(reportRange,
			new Report.GrossRevenue(reportRange, lines),
			new Report.CostOfRevenue(reportRange, lines),
			new Report.OperatingExpenses(reportRange, lines),
			new Report.OtherExpenses(reportRange, lines),
			new Report.OtherIncome(reportRange, lines));
}

public record ProfitAndLossReports(ImmutableArray<ProfitAndLossReport> Reports);

public abstract record Report {
	protected Report(string name, ImmutableArray<Line> lines) {
		Name = name;
		Lines = lines.Where(IsMatch).ToImmutableDictionary(line => line.Account.Number.ToString());
	}

	public decimal Total => Lines.Values.Sum(item => item.Balance);

	protected abstract bool IsMatch(Account account);

	public ImmutableDictionary<string, Line> Lines { get; }

	private bool IsMatch(Line line) => IsMatch(Account.For(new(line.Account.Number)));

	public ImmutableArray<AccountInfo> Accounts => [
		..Lines.Values.Select(line => line.Account)
	];

	public string Name { get; init; }

	public virtual bool Equals(Report? other) => other switch {
		null => false,
		_ => other.GetType() == GetType() && Lines.SequenceEqual(other.Lines)
	};

	public override int GetHashCode() =>
		Lines.Aggregate(0, (hash, line) => HashCode.Combine(hash, line.GetHashCode() ^ 397));

	public record Line(AccountInfo Account, decimal Balance) {
		public static Line Create((int number, string name, decimal balance) item) =>
			new(new AccountInfo(item.number, item.name), item.balance);
	}

	public record AccountInfo(int Number, string Name);

	public record GrossRevenue(ReportRange ReportRange, [property: JsonIgnore] ImmutableArray<Line> lines)
		: Report(nameof(GrossRevenue), lines) {
		protected override bool IsMatch(Account account) => account is IncomeAccount and not OtherIncomeAccount;
	};

	public record CostOfRevenue(ReportRange ReportRange, [property: JsonIgnore] ImmutableArray<Line> lines)
		: Report(nameof(CostOfRevenue), lines) {
		protected override bool IsMatch(Account account) => account is CostOfRevenueExpenseAccount;
	}

	public record OperatingExpenses(ReportRange ReportRange, [property: JsonIgnore] ImmutableArray<Line> lines)
		: Report(nameof(OperatingExpenses), lines) {
		protected override bool IsMatch(Account account) => account is OperatingExpenseAccount;
	}

	public record OtherExpenses(ReportRange ReportRange, [property: JsonIgnore] ImmutableArray<Line> lines)
		: Report(nameof(OtherExpenses), lines) {
		protected override bool IsMatch(Account account) => account is OtherExpenseAccount;
	}

	public record OtherIncome(ReportRange ReportRange, [property: JsonIgnore] ImmutableArray<Line> lines)
		: Report(nameof(OtherIncome), lines) {
		protected override bool IsMatch(Account account) => account is OtherIncomeAccount;
	}
}
