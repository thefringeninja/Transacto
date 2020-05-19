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

		private readonly BalanceSheet _balanceSheet;
		private readonly List<GeneralLedgerEntryIdentifier> _untransferredEntryIdentifiers;
		private readonly List<GeneralLedgerEntryIdentifier> _entryIdentifiers;

		private Period _period;
		private bool _periodClosing;
		private ProfitAndLoss _profitAndLoss;
		private DateTimeOffset _closingOn;
		private GeneralLedgerEntryIdentifier _closingGeneralLedgerEntryIdentifier;

		public override string Id => Identifier;

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
			_balanceSheet = BalanceSheet.None;
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
					_balanceSheet.Apply(new AccountNumber(accountNumber), new Money(amount));
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
			period.Contains(createdOn)
				? new GeneralLedgerEntry(identifier, number, period, createdOn)
				: throw new InvalidOperationException();

		public void BeginClosingPeriod(AccountNumber retainedEarningsAccountNumber,
			GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier,
			GeneralLedgerEntryIdentifier[] generalLedgerEntryIdentifiers, DateTimeOffset closingOn) {
			if (_periodClosing) {
				throw new InvalidOperationException();
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
				throw new InvalidOperationException();
			}

			generalLedgerEntry.MustBeInBalance();
			generalLedgerEntry.MustBePosted();

			_balanceSheet.Transfer(generalLedgerEntry);
			_profitAndLoss.Transfer(generalLedgerEntry);
			_untransferredEntryIdentifiers.Remove(generalLedgerEntry.Identifier);
		}

		public void CompleteClosingPeriod(ChartOfAccounts chartOfAccounts,
			AccountNumber retainedEarningsAccountNumber) {
			if (!_periodClosing) {
				throw new InvalidOperationException();
			}

			if (_untransferredEntryIdentifiers.Count > 0) {
				throw new InvalidOperationException();
			}

			_balanceSheet.MustBeInBalance();

			var closingEntry = _profitAndLoss.GetClosingEntry(chartOfAccounts, retainedEarningsAccountNumber,
				_closingOn, _closingGeneralLedgerEntryIdentifier);

			foreach (var change in closingEntry.GetChanges()) {
				Apply(change);
			}

			_balanceSheet.Transfer(closingEntry);

			_balanceSheet.MustBeInBalance();

			Apply(new AccountingPeriodClosed {
				GeneralLedgerEntryIds = _entryIdentifiers.Select(x => x.ToGuid()).ToArray(),
				ClosingGeneralLedgerEntryId = closingEntry.Identifier.ToGuid(),
				Period = _period.ToString(),
				Balance = _balanceSheet.ToDictionary(x => x.Key.ToInt32(), x => x.Value.ToDecimal())
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

			public GeneralLedgerEntry GetClosingEntry(ChartOfAccounts chartOfAccounts,
				AccountNumber retainedEarningsAccountNumber, DateTimeOffset closedOn,
				GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier) {
				var entry = new GeneralLedgerEntry(closingGeneralLedgerEntryIdentifier,
					new GeneralLedgerEntryNumber($"closingEntry-{_period}"), _period, closedOn);
				foreach (var (accountNumber, amount) in _income) {
					entry.ApplyDebit(new Debit(accountNumber, amount), chartOfAccounts);
				}

				foreach (var (accountNumber, amount) in _expenses) {
					entry.ApplyCredit(new Credit(accountNumber, amount), chartOfAccounts);
				}

				var retainedEarnings = entry.Debits.Select(x => x.Amount).Sum() -
				                       entry.Credits.Select(x => x.Amount).Sum();

				if (retainedEarnings < Money.Zero) {
					entry.ApplyDebit(new Debit(retainedEarningsAccountNumber, retainedEarnings), chartOfAccounts);
				} else if (retainedEarnings > Money.Zero) {
					entry.ApplyCredit(new Credit(retainedEarningsAccountNumber, retainedEarnings), chartOfAccounts);
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

		private class BalanceSheet : IEnumerable<KeyValuePair<AccountNumber, Money>> {
			public static readonly BalanceSheet None = new BalanceSheet();

			private readonly IDictionary<AccountNumber, Money> _inner;

			private BalanceSheet() {
				_inner = new Dictionary<AccountNumber, Money>();
			}

			public void Transfer(GeneralLedgerEntry generalLedgerEntry) {
				foreach (var debit in generalLedgerEntry.Debits.Where(x => x.AppearsOnBalanceSheet)) {
					_inner[debit.AccountNumber] = _inner.TryGetValue(debit.AccountNumber, out var amount)
						? amount + GetBalance(debit)
						: GetBalance(debit);
				}

				foreach (var credit in generalLedgerEntry.Credits.Where(x => x.AppearsOnBalanceSheet)) {
					_inner[credit.AccountNumber] = _inner.TryGetValue(credit.AccountNumber, out var amount)
						? amount + GetBalance(credit)
						: GetBalance(credit);
				}
			}

			public void Apply(AccountNumber accountNumber, Money amount) => _inner[accountNumber] = amount;

			public void MustBeInBalance() {
				if (_inner.Values.Sum() != Money.Zero) {
					throw new InvalidOperationException();
				}
			}

			private static Money GetBalance(Debit debit) => AccountType.OfAccountNumber(debit.AccountNumber) switch {
				AccountType.AssetAccount _ => debit.Amount,
				AccountType.EquityAccount _ => -debit.Amount,
				AccountType.ExpenseAccount _ => -debit.Amount,
				_ => throw new InvalidOperationException()
			};

			private static Money GetBalance(Credit credit) => AccountType.OfAccountNumber(credit.AccountNumber) switch {
				AccountType.AssetAccount _ => -credit.Amount,
				AccountType.EquityAccount _ => credit.Amount,
				AccountType.ExpenseAccount _ => credit.Amount,
				_ => throw new InvalidOperationException()
			};

			public IEnumerator<KeyValuePair<AccountNumber, Money>> GetEnumerator() => _inner.GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_inner).GetEnumerator();
		}
	}
}
