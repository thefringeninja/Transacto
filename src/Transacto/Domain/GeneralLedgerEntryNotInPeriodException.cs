using NodaTime;
using Transacto.Messages;

namespace Transacto.Domain;

public class GeneralLedgerEntryNotInPeriodException(
	GeneralLedgerEntryIdentifier identifier,
	LocalDate createdOn,
	AccountingPeriod accountingPeriod)
	: Exception(FormatMessage(identifier, createdOn, accountingPeriod).ToString()), IFormattableMessage {
	private static FormattableString FormatMessage(GeneralLedgerEntryIdentifier identifier, LocalDate createdOn,
		AccountingPeriod accountingPeriod) =>
		$"General ledger entry {identifier.ToString()} had a creation date of {createdOn:O}, but the current period is {accountingPeriod.ToString()}";

	public GeneralLedgerEntryIdentifier Identifier { get; } = identifier;
	public LocalDate CreatedOn { get; } = createdOn;
	public AccountingPeriod AccountingPeriod { get; } = accountingPeriod;
	public FormattableString ToFormattableString() => FormatMessage(Identifier, CreatedOn, AccountingPeriod);
}
