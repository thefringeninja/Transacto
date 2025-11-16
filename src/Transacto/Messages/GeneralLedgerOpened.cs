namespace Transacto.Messages;

public record GeneralLedgerOpened : IMessage {
	public required string OpenedOn { get; init; }

	public (string template, object[] args) GetMessage() =>
		("The general ledger was opened on {OpenedOn:D}.", [DateOnly.ParseExact(OpenedOn, "yyyyMMdd")]);
}
