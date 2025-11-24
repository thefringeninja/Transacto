using NodaTime;
using Transacto.Messages;

namespace Transacto.Domain;

public class GeneralLedgerEntryNotInPeriodException(
	GeneralLedgerEntryNumber number,
	LocalDateTime createdOn,
	AccountingPeriod accountingPeriod)
	: Exception(FormatMessage(number, createdOn, accountingPeriod).ToString()), IFormattableMessage {
	private static FormattableString FormatMessage(GeneralLedgerEntryNumber number, LocalDateTime createdOn,
		AccountingPeriod accountingPeriod) =>
		$"General ledger entry {number.ToString()} had a creation date of {createdOn:O}, but the current period is {accountingPeriod.ToString()}";

	public GeneralLedgerEntryNumber Number { get; } = number;
	public LocalDateTime CreatedOn { get; } = createdOn;
	public AccountingPeriod AccountingPeriod { get; } = accountingPeriod;
	public FormattableString ToFormattableString() => FormatMessage(Number, CreatedOn, AccountingPeriod);
}
