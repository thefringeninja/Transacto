namespace Transacto.Messages;

partial record DebitApplied : IFormattableMessage {
	public FormattableString ToFormattableString() =>
		$"A debit of {Amount:N2} was applied to account {AccountNumber}.";
}
