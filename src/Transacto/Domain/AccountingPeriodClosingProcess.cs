using System.Collections.Generic;
using System.Linq;
using NodaTime;

namespace Transacto.Domain {
	public class AccountingPeriodClosingProcess {
		private readonly AccountingPeriod _accountingPeriod;
		private readonly LocalDateTime _closingOn;
		private readonly GeneralLedgerEntryIdentifier _closingGeneralLedgerEntryIdentifier;
		private readonly EquityAccount _retainedEarningsAccount;
		private readonly AccountIsDeactivated _accountIsDeactivated;
		private readonly HashSet<GeneralLedgerEntryIdentifier> _generalLedgerEntryIdentifiers;

		public TrialBalance TrialBalance { get; }
		public ProfitAndLoss ProfitAndLoss { get; }

		public AccountingPeriodClosingProcess(
			ChartOfAccounts chartOfAccounts,
			AccountingPeriod accountingPeriod,
			LocalDateTime closingOn,
			GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers,
			GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier,
			EquityAccount retainedEarningsAccount,
			AccountIsDeactivated accountIsDeactivated) {
			_accountingPeriod = accountingPeriod;
			_closingOn = closingOn;
			_closingGeneralLedgerEntryIdentifier = closingGeneralLedgerEntryIdentifier;
			_retainedEarningsAccount = retainedEarningsAccount;
			_accountIsDeactivated = accountIsDeactivated;
			_generalLedgerEntryIdentifiers = new HashSet<GeneralLedgerEntryIdentifier>(generalLedgerEntryIdentifiers);
			TrialBalance = new TrialBalance(chartOfAccounts);
			ProfitAndLoss = new ProfitAndLoss(accountingPeriod, chartOfAccounts);
		}

		public void TransferEntry(GeneralLedgerEntry generalLedgerEntry) {
			generalLedgerEntry.MustBeInBalance();
			generalLedgerEntry.MustBePosted();

			ProfitAndLoss.Transfer(generalLedgerEntry);
			TrialBalance.Transfer(generalLedgerEntry);
			TrialBalance.MustBeInBalance();
			_generalLedgerEntryIdentifiers.Remove(generalLedgerEntry.Identifier);
		}

		public GeneralLedgerEntry Complete() {
			if (_generalLedgerEntryIdentifiers.Count > 0) {
				throw new PeriodContainsUntransferredEntriesException(_accountingPeriod,
					_generalLedgerEntryIdentifiers.ToArray());
			}

			TrialBalance.MustBeInBalance();

			return ProfitAndLoss.GetClosingEntry(_accountIsDeactivated, _retainedEarningsAccount,
				_closingOn, _closingGeneralLedgerEntryIdentifier);
		}
	}
}
