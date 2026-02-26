namespace Transacto.Messages;

partial record AccountingPeriodClosed : IFormattableMessage {
	public FormattableString ToFormattableString() => $"Accounting period {Period} has been closed.";
}
