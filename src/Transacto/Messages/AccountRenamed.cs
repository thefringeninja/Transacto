namespace Transacto.Messages;

public record AccountRenamed : IFormattableMessage {
	public required string NewAccountName { get; init; }
	public required int AccountNumber { get; init; }

	public FormattableString ToFormattableString() => $"Account {AccountNumber} was renamed to {NewAccountName}.";
}
