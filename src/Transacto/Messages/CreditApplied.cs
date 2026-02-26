namespace Transacto.Messages;

partial record CreditApplied : IFormattableMessage {
	public FormattableString ToFormattableString() =>
		$"A credit of {Amount:N2} was applied to account {AccountNumber}.";
}
