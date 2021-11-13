using System;
using System.Collections.Immutable;

namespace Transacto.Messages; 

public partial record BeginClosingAccountingPeriod {
	public BeginClosingAccountingPeriod() {
		GeneralLedgerEntryIds = ImmutableArray<Guid>.Empty;
	}
	public override string ToString() => "Closing accounting period.";
}