using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NodaTime;
using Transacto.Framework;
using Transacto.Messages;

namespace Transacto.Domain {
	public class GeneralLedgerEntry : AggregateRoot {
		public static readonly Func<GeneralLedgerEntry> Factory = () => new GeneralLedgerEntry();

		private State _state;

		private Money Balance => _state.Balance;

		public bool IsInBalance => Balance == Money.Zero;
		public IReadOnlyList<Debit> Debits => _state.Debits;
		public IReadOnlyList<Credit> Credits => _state.Credits;
		public override string Id => FormatStreamIdentifier(_state.Identifier);

		public GeneralLedgerEntryIdentifier Identifier => _state.Identifier;

		public static string FormatStreamIdentifier(GeneralLedgerEntryIdentifier identifier) =>
			$"generalLedgerEntry-{identifier}";

		internal GeneralLedgerEntry(GeneralLedgerEntryIdentifier identifier,
			GeneralLedgerEntryNumber number, AccountingPeriod accountingPeriod, LocalDateTime createdOn) : this() {
			if (!accountingPeriod.Contains(createdOn.Date)) {
				throw new GeneralLedgerEntryNotInPeriodException(number, createdOn, accountingPeriod);
			}

			Apply(new GeneralLedgerEntryCreated {
				GeneralLedgerEntryId = identifier.ToGuid(),
				Number = number.ToString(),
				CreatedOn = Time.Format.LocalDateTime(createdOn),
				Period = accountingPeriod.ToString()
			});
		}

		private GeneralLedgerEntry() {
			_state = new State();
		}

		protected override void ApplyEvent(object _) => _state = _ switch {
			GeneralLedgerEntryCreated e => _state with {
				Identifier = new GeneralLedgerEntryIdentifier(e.GeneralLedgerEntryId),
				AccountingPeriod = AccountingPeriod.Parse(e.Period)
			},
			CreditApplied e => _state with {
				Credits = _state.Credits.Add(new Credit(new AccountNumber(e.AccountNumber), new Money(e.Amount)))
			},
			DebitApplied e => _state with {
				Debits = _state.Debits.Add(new Debit(new AccountNumber(e.AccountNumber), new Money(e.Amount)))
			},
			GeneralLedgerEntryPosted => _state with {
				Posted = true
			},
			_ => _state
		};

		public void ApplyCredit(Credit credit, AccountIsDeactivated accountIsDeactivated) {
			MustNotBePosted();

			if (accountIsDeactivated(credit.AccountNumber)) {
				throw new AccountDeactivatedException(credit.AccountNumber);
			}

			Apply(new CreditApplied {
				GeneralLedgerEntryId = _state.Identifier.ToGuid(),
				Amount = credit.Amount.ToDecimal(),
				AccountNumber = credit.AccountNumber.ToInt32()
			});
		}

		public void ApplyDebit(Debit debit, AccountIsDeactivated accountIsDeactivated) {
			MustNotBePosted();

			if (accountIsDeactivated(debit.AccountNumber)) {
				throw new AccountDeactivatedException(debit.AccountNumber);
			}

			Apply(new DebitApplied {
				GeneralLedgerEntryId = _state.Identifier.ToGuid(),
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
			if (_state.Posted) {
				return;
			}

			MustBeInBalance();

			Apply(new GeneralLedgerEntryPosted {
				GeneralLedgerEntryId = _state.Identifier.ToGuid(),
				Period = _state.AccountingPeriod.ToString()
			});
		}

		private void MustNotBePosted() {
			if (_state.Posted) {
				throw new GeneralLedgerEntryWasPostedException(_state.Identifier);
			}
		}

		public void MustBePosted() {
			if (!_state.Posted) {
				throw new GeneralLedgerEntryWasNotPostedException(_state.Identifier);
			}
		}

		public void MustBeInBalance() {
			if (!IsInBalance) {
				throw new GeneralLedgerEntryNotInBalanceException(_state.Identifier);
			}
		}

		private record State {
			public GeneralLedgerEntryIdentifier Identifier { get; init; }
			public bool Posted { get; init; }
			public ImmutableList<Debit> Debits { get; init; } = ImmutableList<Debit>.Empty;
			public ImmutableList<Credit> Credits { get; init; } = ImmutableList<Credit>.Empty;
			public AccountingPeriod AccountingPeriod { get; init; }

			public Money Balance => Debits.Select(x => x.Amount).Sum() -
			                        Credits.Select(x => x.Amount).Sum();
		}
	}
}
