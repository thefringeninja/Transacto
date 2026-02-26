namespace Transacto.Messages;

partial record GeneralLedgerEntryPosted : IFormattableMessage {
	public FormattableString ToFormattableString() => $"General ledger entry was posted in accounting period {Period}.";
}
