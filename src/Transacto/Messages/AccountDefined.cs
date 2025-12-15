namespace Transacto.Messages;

public record AccountDefined : IFormattableMessage {
	public required string AccountName { get; init; }
	public required int AccountNumber { get; init; }
	public required CashFlowCategoryType CashFlowCategory { get; init; }
	public required bool AffectsCash { get; init; }

	public FormattableString ToFormattableString() => $"Account {AccountNumber} - {AccountName} was defined.";
}
