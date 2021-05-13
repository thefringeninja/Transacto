using System.Collections.Generic;
using System.Linq;
using NodaTime;

namespace Transacto.Domain {
	public class AccountingPeriodClosingProcess {
		private readonly AccountingPeriod _accountingPeriod;
		private readonly LocalDateTime _closingOn;
		private readonly GeneralLedgerEntryIdentifier _closingGeneralLedgerEntryIdentifier;
		private readonly AccountNumber _retainedEarningsAccountNumber;
		private readonly AccountIsDeactivated _accountIsDeactivated;
		private readonly HashSet<GeneralLedgerEntryIdentifier> _generalLedgerEntryIdentifiers;

		public TrialBalance TrialBalance { get; }
		public ProfitAndLoss ProfitAndLoss { get; }

		public AccountingPeriodClosingProcess(AccountingPeriod accountingPeriod,
			LocalDateTime closingOn,
			GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers,
			GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier,
			AccountNumber retainedEarningsAccountNumber,
			AccountIsDeactivated accountIsDeactivated) {
			AccountType.OfAccountNumber(retainedEarningsAccountNumber).MustBe(AccountType.Equity);

			_accountingPeriod = accountingPeriod;
			_closingOn = closingOn;
			_closingGeneralLedgerEntryIdentifier = closingGeneralLedgerEntryIdentifier;
			_retainedEarningsAccountNumber = retainedEarningsAccountNumber;
			_accountIsDeactivated = accountIsDeactivated;
			_generalLedgerEntryIdentifiers = new HashSet<GeneralLedgerEntryIdentifier>(generalLedgerEntryIdentifiers);
			TrialBalance = TrialBalance.None;
			ProfitAndLoss = new ProfitAndLoss(accountingPeriod);
		}

		public void TransferEntry(GeneralLedgerEntry generalLedgerEntry) {
			generalLedgerEntry.MustBeInBalance();
			generalLedgerEntry.MustBePosted();

			TrialBalance.Transfer(generalLedgerEntry);
			TrialBalance.MustBeInBalance();
			ProfitAndLoss.Transfer(generalLedgerEntry);
			_generalLedgerEntryIdentifiers.Remove(generalLedgerEntry.Identifier);
		}

		public GeneralLedgerEntry Complete() {
			if (_generalLedgerEntryIdentifiers.Count > 0) {
				throw new PeriodContainsUntransferredEntriesException(_accountingPeriod,
					_generalLedgerEntryIdentifiers.ToArray());
			}

			TrialBalance.MustBeInBalance();

			return ProfitAndLoss.GetClosingEntry(_accountIsDeactivated, _retainedEarningsAccountNumber,
				_closingOn, _closingGeneralLedgerEntryIdentifier);
		}
	}
}
