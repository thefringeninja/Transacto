using System.Collections.Immutable;
using NodaTime;
using Transacto.Framework;
using Transacto.Messages;

namespace Transacto.Domain;

public class GeneralLedger : AggregateRoot, IAggregateRoot<GeneralLedger> {
	public const string Identifier = "generalLedger";
	public static GeneralLedger Factory() => new();

	private State _state;

	public override string Id { get; } = Identifier;

	public static GeneralLedger Open(LocalDate openedOn) {
		var generalLedger = new GeneralLedger();
		generalLedger.Apply(new GeneralLedgerOpened {
			OpenedOn = openedOn,
		});
		return generalLedger;
	}

	private GeneralLedger() { _state = new State(); }

	protected override void ApplyEvent(object _) => _state = _ switch {
		GeneralLedgerOpened e => _state with {
			AccountingPeriod = AccountingPeriod.Open(e.OpenedOn)
		},
		AccountingPeriodClosing e => _state with {
			PeriodClosing = true,
			AccountingPeriod = AccountingPeriod.Parse(e.Period)
		},
		AccountingPeriodClosed e => _state with {
			PeriodClosing = false,
			AccountingPeriod = AccountingPeriod.Parse(e.Period).Next()
		},
		_ => _state
	};

	public GeneralLedgerEntry Create(GeneralLedgerEntryIdentifier identifier,
		IBusinessTransaction businessTransaction, ChartOfAccounts chartOfAccounts, LocalDate createdOn) => Create(
		identifier, businessTransaction, chartOfAccounts, createdOn,
		(_state.PeriodClosing, _state.AccountingPeriod.Contains(createdOn)) switch {
			(true, _) or (false, false) => _state.AccountingPeriod.Next(),
			_ => _state.AccountingPeriod
		});

	private static GeneralLedgerEntry Create(GeneralLedgerEntryIdentifier identifier,
		IBusinessTransaction businessTransaction, ChartOfAccounts chartOfAccounts, LocalDate createdOn,
		AccountingPeriod accountingPeriod) =>
		new(identifier, businessTransaction, chartOfAccounts, accountingPeriod, createdOn);

	public void BeginClosingPeriod(EquityAccount retainedEarningsAccount,
		GeneralLedgerEntryIdentifier closingGeneralLedgerEntryIdentifier,
		ImmutableArray<GeneralLedgerEntryIdentifier> generalLedgerEntryIdentifiers, LocalDate closingOn) {
		if (_state.PeriodClosing) {
			throw new PeriodClosingInProcessException(_state.AccountingPeriod);
		}

		_state.AccountingPeriod.MustNotBeAfter(closingOn);

		Apply(new AccountingPeriodClosing {
			Period = _state.AccountingPeriod.ToString(),
			GeneralLedgerEntryIds =
				ImmutableArray.CreateRange(generalLedgerEntryIdentifiers, identifier => identifier.ToGuid()),
			ClosingOn = closingOn,
			RetainedEarningsAccountNumber = retainedEarningsAccount.AccountNumber.ToInt32(),
			ClosingGeneralLedgerEntryId = closingGeneralLedgerEntryIdentifier.ToGuid()
		});
	}

	/// <summary>
	/// True when <paramref name="accountingPeriod"/> has already been closed. Used to make replayed closing requests a no-op.
	/// </summary>
	public bool HasClosed(AccountingPeriod accountingPeriod) => accountingPeriod < _state.AccountingPeriod;

	public void CompleteClosingPeriod(AccountingPeriod accountingPeriod,
		ImmutableHashSet<GeneralLedgerEntryIdentifier> generalLedgerEntryIdentifiers,
		GeneralLedgerEntry closingEntry, TrialBalance trialBalance) {
		if (HasClosed(accountingPeriod)) {
			return;
		}

		if (!_state.PeriodClosing || accountingPeriod != _state.AccountingPeriod) {
			throw new PeriodOpenException(_state.AccountingPeriod);
		}

		foreach (var change in closingEntry.GetChanges()) {
			Apply(change);
		}

		trialBalance.Transfer(closingEntry);

		trialBalance.MustBeInBalance();

		Apply(new AccountingPeriodClosed {
			GeneralLedgerEntryIds = ImmutableArray.CreateRange(
				generalLedgerEntryIdentifiers.ToImmutableArray(), identifier => identifier.ToGuid()),
			ClosingGeneralLedgerEntryId = closingEntry.Identifier.ToGuid(),
			Period = _state.AccountingPeriod.ToString(),
			Balance = [
				..trialBalance.Select(account => new BalanceLineItem {
					Amount = account.Balance.ToDecimal(),
					AccountNumber = account.AccountNumber.ToInt32()
				})
			]
		});
	}

	private record State {
		public AccountingPeriod AccountingPeriod { get; init; } = AccountingPeriod.Empty;
		public bool PeriodClosing { get; init; }
	}
}
