using System.Collections.Immutable;
using Transacto.Framework.Projections;
using Transacto.Messages;

namespace Transacto.Plugins.FinancialStatements.CashFlow;

internal record AccountLookup : MemoryReadModel {
	public ImmutableDictionary<int, (string name, DefineAccountCashFlowCategory category, bool affectsCash)>
		Accounts { get; init; } = ImmutableDictionary<int, (string, DefineAccountCashFlowCategory, bool)>.Empty;
}
