namespace Transacto.Messages;

public record AccountDeactivated {
	public required int AccountNumber { get; init; }

	public override string ToString() => $"Account {AccountNumber} was deactivated.";
}
