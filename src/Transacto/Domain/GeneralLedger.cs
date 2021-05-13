using System;
using System.Globalization;
using System.Linq;
using NodaTime;
using Transacto.Framework;
using Transacto.Messages;

namespace Transacto.Domain {
	public class GeneralLedger : AggregateRoot {
		public const string Identifier = "generalLedger";
		public static readonly Func<GeneralLedger> Factory = () => new GeneralLedger();

		private State _state;

		public override string Id { get; } = Identifier;

		public static GeneralLedger Open(LocalDate openedOn) {
			var generalLedger = new GeneralLedger();
			generalLedger.Apply(new GeneralLedgerOpened {
				OpenedOn = Time.Format.LocalDate(openedOn),
			});
			return generalLedger;
		}

		private GeneralLedger() { _state = new State(); }

		protected override void ApplyEvent(object _) => _state = _ switch {
			GeneralLedgerOpened e => _state with {
				AccountingPeriod = AccountingPeriod.Open(Time.Parse.LocalDate(e.OpenedOn))
			},
			AccountingPeriodClosing e => _state with {
				PeriodClosing = true,
				AccountingPeriod = AccountingPeriod.Parse(e.Period)
			},
			AccountingPeriodClosed e => _state with {
				PeriodClosing = false,
				AccountingPeriod = AccountingPeriod.Parse(e.Period)
			},
			_ => _state
		};

		public GeneralLedgerEntry Create(GeneralLedgerEntryIdentifier identifier, GeneralLedgerEntryNumber number,
			LocalDateTime createdOn) => _state.PeriodClosing
			? Create(identifier, number, createdOn, _state.AccountingPeriod.Next())
			: Create(identifier, number, createdOn, _state.AccountingPeriod.Contains(createdOn.Date)
				? _state.AccountingPeriod :
				_state.AccountingPeriod.Next());

		private static GeneralLedgerEntry Create(GeneralLedgerEntryIdentifier identifier,
			GeneralLedgerEntryNumber number, LocalDateTime createdOn, AccountingPeriod accountingPeriod) =>
			new(identifier, number, accountingPeriod, createdOn);

		public void BeginClosingPeriod(AccountNumber retainedEarningsAccountNumber,
			GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier,
			GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers, LocalDateTime closingOn) {
			if (_state.PeriodClosing) {
				throw new PeriodClosingInProcessException(_state.AccountingPeriod);
			}

			AccountType.OfAccountNumber(retainedEarningsAccountNumber).MustBe(AccountType.Equity);

			_state.AccountingPeriod.MustNotBeAfter(closingOn.Date);

			Apply(new AccountingPeriodClosing {
				Period = _state.AccountingPeriod.ToString(),
				GeneralLedgerEntryIds = Array.ConvertAll(generalLedgerEntryIdentifiers, id => id.ToGuid()),
				ClosingOn = Time.Format.LocalDateTime(closingOn),
				RetainedEarningsAccountNumber = retainedEarningsAccountNumber.ToInt32(),
				ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
			});
		}

		public void CompleteClosingPeriod(GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers,
			GeneralLedgerEntry closingEntry, TrialBalance trialBalance) {
			if (!_state.PeriodClosing) {
				throw new PeriodOpenException(_state.AccountingPeriod);
			}

			foreach (var change in closingEntry.GetChanges()) {
				Apply(change);
			}

			trialBalance.Transfer(closingEntry);

			trialBalance.MustBeInBalance();

			Apply(new AccountingPeriodClosed {
				GeneralLedgerEntryIds =
					Array.ConvertAll(generalLedgerEntryIdentifiers, identifier => identifier.ToGuid()),
				ClosingGeneralLedgerEntryId = closingEntry.Identifier.ToGuid(),
				Period = _state.AccountingPeriod.ToString(),
				Balance = trialBalance.Select(x => new BalanceLineItem {
					Amount = x.Value.ToDecimal(),
					AccountNumber = x.Key.ToInt32()
				}).ToArray()
			});
		}

		private record State {
			public AccountingPeriod AccountingPeriod { get; init; } = AccountingPeriod.Empty;
			public bool PeriodClosing { get; init; }
		}
	}
}
