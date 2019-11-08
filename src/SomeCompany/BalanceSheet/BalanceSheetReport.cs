using System;
using System.Linq;

namespace SomeCompany.BalanceSheet {
    public class BalanceSheetReport {
        public DateTime Thru { get; set; }
        public LineItemGrouping[] Groupings { get; set; }

        public class LineItemGrouping {
            public LineItemGrouping[] Groupings { get; set; }
            public LineItem[] LineItems { get; set; }
            public string Name { get; set; }
            public decimal Total => LineItems.Sum(x => x.Balance);
        }
        public class LineItem {
            public string Name { get; set; }
            public int AccountNumber { get; set; }
            public decimal Balance { get; set; }
        }
    }
}
