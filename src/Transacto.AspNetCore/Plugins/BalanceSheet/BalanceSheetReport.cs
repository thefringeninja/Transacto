using System.Collections.Generic;
using System.Linq;
using Transacto.Domain;

namespace Transacto.Plugins.BalanceSheet {
    partial class LineItemGrouping {
        public decimal Total => LineItems.Sum(x => x.Balance.DecimalValue);

        public LineItemGrouping() {
	        Name = null!;
	        LineItems = new List<LineItem>();
	        LineItemGroupings = new List<LineItemGrouping>();
        }
    }

    partial class LineItem {
	    public LineItem() {
		    Name = null!;
		    Balance = new Decimal();
	    }
    }

    partial class Decimal {
	    public Decimal() {
		    Value = "0";
	    }
	    public decimal DecimalValue {
		    get => decimal.TryParse(Value, out var value) ? value : decimal.Zero;
		    set => Value = value.ToString();
	    }
    }

    partial class BalanceSheetReport {
	    public BalanceSheetReport() {
		    LineItemGroupings = new List<LineItemGrouping>();
		    LineItems = new List<LineItem>();
	    }

	    public decimal TotalAssets => LineItems.Where(x =>
			    AccountType.OfAccountNumber(new AccountNumber(x.AccountNumber)) is AccountType.AssetAccount)
		    .Sum(x => x.Balance.DecimalValue);
	    public decimal TotalLiabilities => LineItems.Where(x =>
			    AccountType.OfAccountNumber(new AccountNumber(x.AccountNumber)) is AccountType.LiabilityAccount)
		    .Sum(x => x.Balance.DecimalValue);

	    public decimal TotalEquity => LineItems.Where(x =>
			    AccountType.OfAccountNumber(new AccountNumber(x.AccountNumber)) is AccountType.EquityAccount)
		    .Sum(x => x.Balance.DecimalValue);
    }
}
