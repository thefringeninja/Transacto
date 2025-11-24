namespace Transacto.Messages;

public record GeneralLedgerOpened : IFormattableMessage {
	public required string OpenedOn { get; init; }

	public FormattableString ToFormattableString() =>
		$"The general ledger was opened on {OpenedOn:D}.";
}
