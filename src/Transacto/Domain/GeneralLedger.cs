using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Transacto.Framework;
using Transacto.Messages;

namespace Transacto.Domain {
	public class GeneralLedger : AggregateRoot {
		public const string Identifier = "generalLedger";
		public static readonly Func<GeneralLedger> Factory = () => new GeneralLedger();

		private readonly TrialBalance _trialBalance;
		private readonly List<GeneralLedgerEntryIdentifier> _untransferredEntryIdentifiers;
		private readonly List<GeneralLedgerEntryIdentifier> _entryIdentifiers;

		private Period _period;
		private bool _periodClosing;
		private ProfitAndLoss _profitAndLoss;
		private DateTimeOffset _closingOn;
		private GeneralLedgerEntryIdentifier _closingGeneralLedgerEntryIdentifier;

		public override string Id { get; } = Identifier;

		public static GeneralLedger Open(DateTimeOffset openedOn) {
			var generalLedger = new GeneralLedger();
			generalLedger.Apply(new GeneralLedgerOpened {
				OpenedOn = openedOn
			});
			return generalLedger;
		}

		private GeneralLedger() {
			_untransferredEntryIdentifiers = new List<GeneralLedgerEntryIdentifier>();
			_entryIdentifiers = new List<GeneralLedgerEntryIdentifier>();
			_trialBalance = TrialBalance.None;
			_closingOn = default;
			_profitAndLoss = null!;

			Register<GeneralLedgerOpened>(e => _period = Period.Open(e.OpenedOn));
			Register<AccountingPeriodClosing>(e => {
				_periodClosing = true;
				_period = Period.Parse(e.Period);
				_profitAndLoss = new ProfitAndLoss(_period);
				_entryIdentifiers.AddRange(Array.ConvertAll(e.GeneralLedgerEntryIds, identifier =>
					new GeneralLedgerEntryIdentifier(identifier)));
				_untransferredEntryIdentifiers.AddRange(_entryIdentifiers);
				_closingOn = e.ClosingOn;
				_closingGeneralLedgerEntryIdentifier = new GeneralLedgerEntryIdentifier(e.ClosingGeneralLedgerEntryId);
			});
			Register<AccountingPeriodClosed>(e => {
				_untransferredEntryIdentifiers.Clear();
				_entryIdentifiers.Clear();
				foreach (var (accountNumber, amount) in e.Balance) {
					_trialBalance.Apply(new AccountNumber(accountNumber), new Money(amount));
				}

				_period = Period.Parse(e.Period).Next();
				_periodClosing = false;
			});
		}

		public GeneralLedgerEntry Create(GeneralLedgerEntryIdentifier identifier, GeneralLedgerEntryNumber number,
			DateTimeOffset createdOn) => _periodClosing
			? Create(identifier, number, createdOn, _period.Next())
			: Create(identifier, number, createdOn, _period.Contains(createdOn) ? _period : _period.Next());

		private static GeneralLedgerEntry Create(GeneralLedgerEntryIdentifier identifier,
			GeneralLedgerEntryNumber number, DateTimeOffset createdOn, Period period) =>
			new GeneralLedgerEntry(identifier, number, period, createdOn);

		public void BeginClosingPeriod(AccountNumber retainedEarningsAccountNumber,
			GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier,
			GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers, DateTimeOffset closingOn) {
			if (_periodClosing) {
				throw new PeriodClosingInProcessException(_period);
			}

			_period.MustNotBeAfter(closingOn);

			Apply(new AccountingPeriodClosing {
				Period = _period.ToString(),
				GeneralLedgerEntryIds = Array.ConvertAll(generalLedgerEntryIdentifiers, id => id.ToGuid()),
				ClosingOn = closingOn,
				RetainedEarningsAccountNumber = retainedEarningsAccountNumber.ToInt32(),
				ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
			});
		}

		public void TransferEntry(GeneralLedgerEntry generalLedgerEntry) {
			if (!_periodClosing) {
				throw new PeriodClosingInProcessException(_period);
			}

			generalLedgerEntry.MustBeInBalance();
			generalLedgerEntry.MustBePosted();

			_trialBalance.Transfer(generalLedgerEntry);
			_profitAndLoss.Transfer(generalLedgerEntry);
			_untransferredEntryIdentifiers.Remove(generalLedgerEntry.Identifier);
		}

		public void CompleteClosingPeriod(AccountIsDeactivated accountIsDeactivated,
			AccountNumber retainedEarningsAccountNumber) {
			if (!_periodClosing) {
				throw new PeriodOpenException(_period);
			}

			if (_untransferredEntryIdentifiers.Count > 0) {
				throw new PeriodContainsUntransferredEntriesException(_period,
					_untransferredEntryIdentifiers.ToArray());
			}

			_trialBalance.MustBeInBalance();

			var closingEntry = _profitAndLoss.GetClosingEntry(accountIsDeactivated, retainedEarningsAccountNumber,
				_closingOn, _closingGeneralLedgerEntryIdentifier);

			foreach (var change in closingEntry.GetChanges()) {
				Apply(change);
			}

			_trialBalance.Transfer(closingEntry);

			_trialBalance.MustBeInBalance();

			Apply(new AccountingPeriodClosed {
				GeneralLedgerEntryIds = _entryIdentifiers.Select(x => x.ToGuid()).ToArray(),
				ClosingGeneralLedgerEntryId = closingEntry.Identifier.ToGuid(),
				Period = _period.ToString(),
				Balance = _trialBalance.Select(x => new BalanceLineItem {
					Amount = x.Value.ToDecimal(),
					AccountNumber = x.Key.ToInt32()
				}).ToArray()
			});
		}

		private class ProfitAndLoss {
			private readonly Period _period;
			private readonly IDictionary<AccountNumber, Money> _income;
			private readonly IDictionary<AccountNumber, Money> _expenses;

			public ProfitAndLoss(Period period) {
				_period = period;
				_income = new Dictionary<AccountNumber, Money>();
				_expenses = new Dictionary<AccountNumber, Money>();
			}

			public GeneralLedgerEntry GetClosingEntry(AccountIsDeactivated accountIsDeactivated,
				AccountNumber retainedEarningsAccountNumber, DateTimeOffset closedOn,
				GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) {
				var entry = new GeneralLedgerEntry(closingGeneralLedgerEntryIdentifier,
					new GeneralLedgerEntryNumber("jec", int.Parse(_period.ToString())), _period, closedOn);
				foreach (var (accountNumber, amount) in _income) {
					if (amount == Money.Zero) {
						continue;
					}

					if (amount > Money.Zero) {
						entry.ApplyCredit(new Credit(accountNumber, amount), accountIsDeactivated);
					} else {
						entry.ApplyDebit(new Debit(accountNumber, -amount), accountIsDeactivated);
					}
				}

				foreach (var (accountNumber, amount) in _expenses) {
					if (amount == Money.Zero) {
						continue;
					}

					if (amount < Money.Zero) {
						entry.ApplyCredit(new Credit(accountNumber, amount), accountIsDeactivated);
					} else {
						entry.ApplyDebit(new Debit(accountNumber, -amount), accountIsDeactivated);
					}
				}

				var retainedEarnings = entry.Debits.Select(x => x.Amount).Sum() -
				                       entry.Credits.Select(x => x.Amount).Sum();

				if (retainedEarnings < Money.Zero) {
					entry.ApplyDebit(new Debit(retainedEarningsAccountNumber, -retainedEarnings), accountIsDeactivated);
				} else if (retainedEarnings > Money.Zero) {
					entry.ApplyCredit(new Credit(retainedEarningsAccountNumber, retainedEarnings),
						accountIsDeactivated);
				}

				entry.Post();

				return entry;
			}

			public void Transfer(GeneralLedgerEntry generalLedgerEntry) {
				foreach (var credit in generalLedgerEntry.Credits) {
					var accountType = AccountType.OfAccountNumber(credit.AccountNumber);
					if (accountType is AccountType.ExpenseAccount) {
						_expenses[credit.AccountNumber] = _expenses.TryGetValue(credit.AccountNumber, out var amount)
							? amount + credit.Amount
							: credit.Amount;
					} else if (accountType is AccountType.IncomeAccount) {
						_income[credit.AccountNumber] = _income.TryGetValue(credit.AccountNumber, out var amount)
							? amount - credit.Amount
							: -credit.Amount;
					}
				}

				foreach (var debit in generalLedgerEntry.Debits) {
					var accountType = AccountType.OfAccountNumber(debit.AccountNumber);
					if (accountType is AccountType.ExpenseAccount) {
						_expenses[debit.AccountNumber] = _expenses.TryGetValue(debit.AccountNumber, out var amount)
							? amount - debit.Amount
							: -debit.Amount;
					} else if (accountType is AccountType.IncomeAccount) {
						_income[debit.AccountNumber] = _income.TryGetValue(debit.AccountNumber, out var amount)
							? amount + debit.Amount
							: debit.Amount;
					}
				}
			}
		}

		private class TrialBalance : IEnumerable<KeyValuePair<AccountNumber, Money>> {
			public static TrialBalance None => new TrialBalance();

			private readonly IDictionary<AccountNumber, Money> _inner;

			private TrialBalance() {
				_inner = new Dictionary<AccountNumber, Money>();
			}

			public void Transfer(GeneralLedgerEntry generalLedgerEntry) {
				foreach (var debit in generalLedgerEntry.Debits) {
					_inner[debit.AccountNumber] = _inner.TryGetValue(debit.AccountNumber, out var amount)
						? amount + debit.Amount
						: debit.Amount;
				}

				foreach (var credit in generalLedgerEntry.Credits) {
					_inner[credit.AccountNumber] = _inner.TryGetValue(credit.AccountNumber, out var amount)
						? amount - credit.Amount
						: -credit.Amount;
				}
			}

			public void Apply(AccountNumber accountNumber, Money amount) => _inner[accountNumber] = amount;

			public void MustBeInBalance() {
				var balance = _inner.Values.Sum();
				if (balance != Money.Zero) {
					throw new TrialBalanceFailedException(balance);
				}
			}

			public IEnumerator<KeyValuePair<AccountNumber, Money>> GetEnumerator() => _inner.GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_inner).GetEnumerator();
		}
	}
}
