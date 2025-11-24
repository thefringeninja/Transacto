using NodaTime;
using Transacto.Domain;
using Transacto.Testing;
using static Transacto.Specifications.GeneralLedgerEntrySpecifications;

namespace Transacto.Application;

public class GeneralLedgerEntryTests {
	private readonly IFactRecorder _facts;
	private readonly GetPrefix _getPrefix;

	public GeneralLedgerEntryTests() {
		_facts = new FactRecorder();
		_getPrefix = t => new GeneralLedgerEntryNumberPrefix(t.GetType().Name switch {
			nameof(BadTransaction) => "bad",
			nameof(TestTransaction) => "t",
			_ => "x"
		});
	}

	[AutoFixtureData]
	public Task posting_an_entry(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
		GeneralLedgerEntrySequenceNumber sequenceNumber, LocalDate openedOn, AssetAccount assetAccount,
		LiabilityAccount liabilityAccount, Money amount) => new PostingAnEntry(generalLedgerEntryIdentifier,
			sequenceNumber, openedOn, assetAccount, liabilityAccount, amount)
		.Assert(new GeneralLedgerEntryHandlers(new GeneralLedgerTestRepository(_facts),
			new GeneralLedgerEntryTestRepository(_facts), _getPrefix, _ => false), _facts);

	[AutoFixtureData]
	public Task entry_date_not_in_current_or_next_period_throws(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier, GeneralLedgerEntrySequenceNumber sequenceNumber,
		LocalDate openedOn) =>
		new EntryDateNotInCurrentOrNextPeriodThrows(generalLedgerEntryIdentifier, sequenceNumber, openedOn)
			.Assert(new GeneralLedgerEntryHandlers(new GeneralLedgerTestRepository(_facts),
				new GeneralLedgerEntryTestRepository(_facts), _getPrefix, _ => false), _facts);

	[AutoFixtureData]
	public Task applying_debit_to_non_existing_account_throws(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier, GeneralLedgerEntryNumber referenceNumber,
		LocalDate openedOn, AccountNumber accountNumber) =>
		new ApplyingDebitToNonExistingAccountThrows(generalLedgerEntryIdentifier, referenceNumber, openedOn,
				accountNumber)
			.Assert(new GeneralLedgerEntryHandlers(new GeneralLedgerTestRepository(_facts),
				new GeneralLedgerEntryTestRepository(_facts), _getPrefix, _ => true), _facts);

	[AutoFixtureData]
	public Task applying_credit_to_non_existing_account_throws(
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
		GeneralLedgerEntrySequenceNumber sequenceNumber, LocalDate openedOn, AccountNumber accountNumber) =>
		new ApplyingCreditToNonExistingAccountThrows(generalLedgerEntryIdentifier, sequenceNumber, openedOn,
			accountNumber).Assert(new GeneralLedgerEntryHandlers(new GeneralLedgerTestRepository(_facts),
			new GeneralLedgerEntryTestRepository(_facts), _getPrefix, _ => true), _facts);

	[AutoFixtureData]
	public Task entry_not_in_balance_throws(GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
		GeneralLedgerEntrySequenceNumber sequenceNumber, LocalDate openedOn, AccountName accountName,
		AccountNumber accountNumber) =>
		new EntryNotInBalanceThrows(generalLedgerEntryIdentifier, sequenceNumber, openedOn, Account.For(accountNumber, accountName))
			.Assert(
				new GeneralLedgerEntryHandlers(new GeneralLedgerTestRepository(_facts),
					new GeneralLedgerEntryTestRepository(_facts), _getPrefix, _ => false), _facts);
}
