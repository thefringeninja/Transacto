using System;
using Transacto.Framework;
using Transacto.Messages;

namespace Transacto.Domain {
	public class GeneralLedgerEntry : AggregateRoot {
		public static readonly Func<GeneralLedgerEntry> Factory = () => new GeneralLedgerEntry();
		private GeneralLedgerEntryIdentifier _identifier;
		private Money _balance;
		private bool _posted;

		public GeneralLedgerEntryIdentifier GeneralLedgerEntryIdentifier => _identifier;
		public bool IsInBalance => _balance == Money.Zero;

		internal GeneralLedgerEntry(GeneralLedgerEntryIdentifier identifier,
			GeneralLedgerEntryNumber number, PeriodIdentifier period, DateTimeOffset createdOn) : this() {
			Apply(new GeneralLedgerEntryCreated {
				GeneralLedgerEntryId = identifier.ToGuid(),
				Number = number.ToString(),
				CreatedOn = createdOn,
				Period = period.ToDto()
			});
		}

		private GeneralLedgerEntry() {
			Register<GeneralLedgerEntryCreated>(e =>
				_identifier = new GeneralLedgerEntryIdentifier(e.GeneralLedgerEntryId));
			Register<CreditApplied>(e => _balance += new Money(e.Amount));
			Register<DebitApplied>(e => _balance -= new Money(e.Amount));
			Register<GeneralLedgerEntryPosted>(_ => _posted = true);
		}

		public void ApplyCredit(Credit credit) {
			MustNotBePosted();

			Apply(new CreditApplied {
				GeneralLedgerEntryId = _identifier.ToGuid(),
				Amount = credit.Amount.ToDecimal(),
				AccountNumber = credit.AccountNumber.ToInt32()
			});
		}

		public void ApplyDebit(Debit debit) {
			MustNotBePosted();

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
				GeneralLedgerEntryId = _identifier.ToGuid()
			});
		}

		private void MustNotBePosted() {
			if (!_posted) return;

			throw new InvalidOperationException();
		}


		private void MustBeInBalance() {
			if (_balance == Money.Zero) return;

			throw new InvalidOperationException();
		}
	}
}
