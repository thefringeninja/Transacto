using Transacto.Domain;

namespace Transacto.Plugins.BalanceSheet;

sealed partial record BalanceSheetReport {
	public decimal TotalAssets => LineItems
		.Where(x => Account.For(new AccountNumber(x.AccountNumber)) is AssetAccount)
		.Sum(x => x.Balance.DecimalValue);

	public decimal TotalLiabilities => LineItems
		.Where(x => Account.For(new AccountNumber(x.AccountNumber)) is LiabilityAccount)
		.Sum(x => x.Balance.DecimalValue);

	public decimal TotalEquity => TotalAssets - TotalLiabilities;

	public bool Equals(BalanceSheetReport? other) => other switch {
		null => false,
		_ => ReferenceEquals(this, other) || Thru.Equals(other.Thru) && LineItems.SequenceEqual(other.LineItems) &&
			LineItemGroupings.SequenceEqual(other.LineItemGroupings)
	};

	public override int GetHashCode() => HashCode.Combine(Thru,
		LineItems.Aggregate(397, (i, item) => HashCode.Combine(i, item.GetHashCode())),
		LineItemGroupings.Aggregate(397, (i, item) => HashCode.Combine(i, item.GetHashCode())));
}
