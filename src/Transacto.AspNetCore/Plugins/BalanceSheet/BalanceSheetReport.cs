using System.Collections.Generic;
using System.Linq;
using Transacto.Domain;

namespace Transacto.Plugins.BalanceSheet {
    partial record LineItemGrouping {
        public decimal Total => LineItems.Sum(x => x.Balance.DecimalValue);

        public LineItemGrouping() {
	        Name = null!;
	        LineItems = new List<LineItem>();
	        LineItemGroupings = new List<LineItemGrouping>();
        }
    }

    partial record LineItem {
	    public LineItem() {
		    Name = null!;
		    Balance = new Decimal();
	    }
    }

    partial record Decimal {
	    public Decimal() {
		    Value = "0";
	    }
	    public decimal DecimalValue {
		    get => decimal.TryParse(Value, out var value) ? value : decimal.Zero;
		    init => Value = value.ToString();
	    }
    }

    partial record BalanceSheetReport {
	    public BalanceSheetReport() {
		    LineItemGroupings = new List<LineItemGrouping>();
		    LineItems = new List<LineItem>();
	    }

	    public decimal TotalAssets => LineItems
		    .Where(x => Account.For(default, new AccountNumber(x.AccountNumber)) is AssetAccount)
		    .Sum(x => x.Balance.DecimalValue);
	    public decimal TotalLiabilities => LineItems
		    .Where(x => Account.For(default, new AccountNumber(x.AccountNumber)) is LiabilityAccount)
		    .Sum(x => x.Balance.DecimalValue);

	    public decimal TotalEquity => LineItems
		    .Where(x => Account.For(default, new AccountNumber(x.AccountNumber)) is EquityAccount)
		    .Sum(x => x.Balance.DecimalValue);
    }
}
