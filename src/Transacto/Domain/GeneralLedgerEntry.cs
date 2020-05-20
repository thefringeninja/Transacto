using System;
using System.Collections.Generic;
using System.Linq;
using Transacto.Framework;
using Transacto.Messages;

namespace Transacto.Domain {
	public class GeneralLedgerEntry : AggregateRoot {
		public static readonly Func<GeneralLedgerEntry> Factory = () => new GeneralLedgerEntry();

		private GeneralLedgerEntryIdentifier _identifier;

		private bool _posted;
		private readonly List<Debit> _debits;
		private readonly List<Credit> _credits;
		private Period _period;

		private Money Balance => _debits.Select(x => x.Amount).Sum() -
		                         _credits.Select(x => x.Amount).Sum();

		public bool IsInBalance => Balance == Money.Zero;
		public IEnumerable<Debit> Debits => _debits.AsReadOnly();
		public IEnumerable<Credit> Credits => _credits.AsReadOnly();
		public override string Id => FormatStreamIdentifier(_identifier);

		public GeneralLedgerEntryIdentifier Identifier => _identifier;

		public static string FormatStreamIdentifier(GeneralLedgerEntryIdentifier identifier) =>
			$"generalLedgerEntry-{identifier}";

		internal GeneralLedgerEntry(GeneralLedgerEntryIdentifier identifier,
			GeneralLedgerEntryNumber number, Period period, DateTimeOffset createdOn) : this() {
			if (!period.Contains(createdOn)) {
				throw new GeneralLedgerEntryNotInPeriodException(number, createdOn, period);
			}

			Apply(new GeneralLedgerEntryCreated {
				GeneralLedgerEntryId = identifier.ToGuid(),
				Number = number.ToString(),
				CreatedOn = createdOn,
				Period = period.ToString()
			});
		}

		private GeneralLedgerEntry() {
			_credits = new List<Credit>();
			_debits = new List<Debit>();

			Register<GeneralLedgerEntryCreated>(e => {
				_identifier = new GeneralLedgerEntryIdentifier(e.GeneralLedgerEntryId);
				_period = Period.Parse(e.Period);
			});
			Register<CreditApplied>(e =>
				_credits.Add(new Credit(new AccountNumber(e.AccountNumber), new Money(e.Amount))));
			Register<DebitApplied>(e =>
				_debits.Add(new Debit(new AccountNumber(e.AccountNumber), new Money(e.Amount))));
			Register<GeneralLedgerEntryPosted>(_ => _posted = true);
		}

		public void ApplyCredit(Credit credit, ChartOfAccounts chartOfAccounts) {
			MustNotBePosted();

			chartOfAccounts.MustNotBeDeactivated(credit.AccountNumber);

			Apply(new CreditApplied {
				GeneralLedgerEntryId = _identifier.ToGuid(),
				Amount = credit.Amount.ToDecimal(),
				AccountNumber = credit.AccountNumber.ToInt32()
			});
		}

		public void ApplyDebit(Debit debit, ChartOfAccounts chartOfAccounts) {
			MustNotBePosted();

			chartOfAccounts.MustNotBeDeactivated(debit.AccountNumber);

			Apply(new DebitApplied {
				GeneralLedgerEntryId = _identifier.ToGuid(),
				Amount = debit.Amount.ToDecimal(),
				AccountNumber = debit.AccountNumber.ToInt32()
			});
		}

		public void ApplyTransaction(IBusinessTransaction transaction) {
			MustNotBePosted();

			foreach (var x in transaction.GetAdditionalChanges()) {
				Apply(x);
			}
		}

		public void Post() {
			if (_posted) {
				return;
			}

			MustBeInBalance();

			Apply(new GeneralLedgerEntryPosted {
				GeneralLedgerEntryId = _identifier.ToGuid(),
				Period = _period.ToString()
			});
		}

		private void MustNotBePosted() {
			if (_posted) {
				throw new GeneralLedgerEntryWasPostedException(_identifier);
			}
		}

		public void MustBePosted() {
			if (!_posted) {
				throw new GeneralLedgerEntryWasNotPostedException(_identifier);
			}
		}

		public void MustBeInBalance() {
			if (!IsInBalance) {
				throw new GeneralLedgerEntryNotInBalanceException(_identifier);
			}
		}
	}
}
