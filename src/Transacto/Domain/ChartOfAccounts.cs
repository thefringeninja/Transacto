using System.Collections.Immutable;
using System.Security.Principal;
using Transacto.Framework;
using Transacto.Messages;

namespace Transacto.Domain;

public class ChartOfAccounts : AggregateRoot, IAggregateRoot<ChartOfAccounts> {
	public const string Identifier = "chartOfAccounts";
	public static ChartOfAccounts Factory() => new();

	public override string Id { get; } = Identifier;

	private State _state;

	public Account this[AccountNumber accountNumber] =>
		_state.Accounts.TryGetValue(accountNumber, out var info)
			? info.Account
			: throw new AccountNotFoundException(accountNumber);

	private ChartOfAccounts() {
		_state = new State();
	}

	public TAccount Get<TAccount>(AccountNumber accountNumber) where TAccount : Account =>
		this[accountNumber] is TAccount account ? account : throw new AccountNotFoundException(accountNumber);

	protected override void ApplyEvent(object _) => _state = _ switch {
		AccountDefined e => _state with {
			Accounts = _state.Accounts.Add(new AccountNumber(e.AccountNumber),
				new(Account.For(new AccountNumber(e.AccountNumber), new AccountName(e.AccountName))))
		},
		AccountDeactivated e => _state with {
			Accounts = _state.Accounts.SetItem(new AccountNumber(e.AccountNumber),
				_state.Accounts[new AccountNumber(e.AccountNumber)] with {
					IsActive = false
				})
		},
		AccountReactivated e => _state with {
			Accounts = _state.Accounts.SetItem(new AccountNumber(e.AccountNumber),
				_state.Accounts[new AccountNumber(e.AccountNumber)] with {
					IsActive = true
				})
		},
		AccountRenamed e => _state with {
			Accounts = _state.Accounts.SetItem(new AccountNumber(e.AccountNumber),
				_state.Accounts[new AccountNumber(e.AccountNumber)] with {
					Account = Account.For(new AccountNumber(e.AccountNumber), new AccountName(e.NewAccountName))
				})
		},
		_ => _state
	};

	public void DefineAccount(AccountName accountName, AccountNumber accountNumber) {
		MustNotContainAccountNumber(accountNumber);

		Apply(new AccountDefined {
			AccountName = accountName.ToString(),
			AccountNumber = accountNumber.ToInt32()
		});
	}

	public void DeactivateAccount(AccountNumber accountNumber) {
		MustContainAccountNumber(accountNumber);

		if (IsInactive(accountNumber)) {
			return;
		}

		Apply(new AccountDeactivated {
			AccountNumber = accountNumber.ToInt32()
		});
	}

	public void ReactivateAccount(AccountNumber accountNumber) {
		MustContainAccountNumber(accountNumber);

		if (IsActive(accountNumber)) {
			return;
		}

		Apply(new AccountReactivated {
			AccountNumber = accountNumber.ToInt32()
		});
	}

	public void RenameAccount(AccountNumber accountNumber, AccountName newAccountName) {
		MustContainAccountNumber(accountNumber);

		Apply(new AccountRenamed {
			AccountNumber = accountNumber.ToInt32(),
			NewAccountName = newAccountName.ToString()
		});
	}

	private void MustNotContainAccountNumber(AccountNumber accountNumber) {
		if (_state.Accounts.ContainsKey(accountNumber)) {
			throw new AccountExistsException(accountNumber);
		}
	}

	private void MustContainAccountNumber(AccountNumber accountNumber) {
		if (!_state.Accounts.ContainsKey(accountNumber)) {
			throw new AccountNotFoundException(accountNumber);
		}
	}

	private bool IsInactive(AccountNumber accountNumber) =>
		_state.Accounts.TryGetValue(accountNumber, out var account)
		&& !account.IsActive;

	private bool IsActive(AccountNumber accountNumber) =>
		_state.Accounts.TryGetValue(accountNumber, out var account)
		&& account.IsActive;

	private record State {
		public ImmutableDictionary<AccountNumber, AccountInfo> Accounts { get; init; }
			= ImmutableDictionary<AccountNumber, AccountInfo>.Empty;

		public record AccountInfo(Account Account, bool IsActive = true);
	}
}
