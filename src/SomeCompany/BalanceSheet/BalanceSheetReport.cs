using System;
using System.Linq;

namespace SomeCompany.BalanceSheet {
    partial class LineItemGrouping {
        public decimal Total => LineItems.Sum(x => Convert.ToDecimal(x.Balance));
    }
}
