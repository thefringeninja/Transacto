namespace Transacto.Messages;

partial record AccountingPeriodClosing : IFormattableMessage {
	public FormattableString ToFormattableString() => $"Accounting period {Period} is closing.";
}
