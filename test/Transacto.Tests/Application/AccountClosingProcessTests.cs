using NodaTime;
using Transacto.Domain;
using Transacto.Testing;
using static Transacto.Specifications.AccountClosingProcessSpecifications;

namespace Transacto.Application;

public class AccountClosingProcessTests {
	private readonly AccountingPeriodClosingHandlers _handler;
	private readonly IFactRecorder _facts;

	public AccountClosingProcessTests() {
		_facts = new FactRecorder();
		_handler = new AccountingPeriodClosingHandlers(
			new GeneralLedgerTestRepository(_facts),
			new GeneralLedgerEntryTestRepository(_facts),
			new ChartOfAccountsTestRepository(_facts));
	}

	[AutoFixtureData]
	public Task unposted_entry_throws(LocalDate openedOn, EquityAccount retainedEarnings,
		GeneralLedgerEntryIdentifier generalLedgerEntryIdentifier,
		GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) =>
		new UnpostedEntryThrows(openedOn, retainedEarnings, generalLedgerEntryIdentifier,
				closingGeneralLedgerEntryIdentifier)
			.Assert(_handler, _facts);

	[AutoFixtureData]
	public Task period_closing_started(LocalDate openedOn, EquityAccount retainedEarnings, AssetAccount cashAccount,
		AssetAccount accountsReceivable, LiabilityAccount accountsPayable, IncomeAccount incomeAccount,
		ExpenseAccount expenseAccount, GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers,
		GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier, Money income, Money expense) =>
		new PeriodClosingStarted(openedOn, retainedEarnings, cashAccount, accountsReceivable, accountsPayable,
				incomeAccount, expenseAccount, generalLedgerEntryIdentifiers, closingGeneralLedgerEntryIdentifier,
				income, expense)
			.Assert(_handler, _facts);
}
