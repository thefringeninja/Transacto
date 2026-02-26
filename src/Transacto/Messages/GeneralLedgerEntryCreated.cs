namespace Transacto.Messages;

partial record GeneralLedgerEntryCreated : IFormattableMessage {
	public FormattableString ToFormattableString() =>
		$"General ledger entry {GeneralLedgerEntryId} was created in accounting period {Period}.";
}
