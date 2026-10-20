using Hallo;

namespace Transacto.Plugins.GeneralLedger;

internal class JournalEntryRepresentation : Hal<JournalLedgerEntry>, IHalState<JournalLedgerEntry> {
	public object StateFor(JournalLedgerEntry resource) => resource;
}
