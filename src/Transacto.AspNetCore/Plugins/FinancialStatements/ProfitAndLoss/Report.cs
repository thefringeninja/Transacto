using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Transacto.Domain;

namespace Transacto.Plugins.FinancialStatements.ProfitAndLoss;

public abstract record Report {
	public ImmutableSortedDictionary<int, Line> Lines { get; }

	public string Name { get; }

	protected Report(string name, ImmutableArray<Line> lines) {
		Name = name;
		Lines = lines.Where(IsMatch).ToImmutableSortedDictionary(line => line.Account.Number, line => line);
	}

	public decimal Total => Lines.Values.Sum(item => item.Balance);

	protected abstract bool IsMatch(Account account);

	private bool IsMatch(Line line) => IsMatch(Account.For(new(line.Account.Number)));

	[JsonIgnore]
	public ImmutableArray<AccountInfo> Accounts => [
		..Lines.Values.Select(line => line.Account).Distinct()
	];


	public virtual bool Equals(Report? other) => other switch {
		null => false,
		_ => other.GetType() == GetType() && Lines.SequenceEqual(other.Lines)
	};

	public override int GetHashCode() =>
		Lines.Aggregate(0, (hash, line) => HashCode.Combine(hash, line.GetHashCode() ^ 397));

	public record Line(AccountInfo Account, decimal Balance) {
		public static Line Create((int number, string name, decimal balance) item) =>
			new(new AccountInfo(item.number, item.name), item.balance / 100m);
	}

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
