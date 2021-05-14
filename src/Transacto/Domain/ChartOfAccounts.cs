using System;
using System.Collections.Immutable;
using Transacto.Framework;
using Transacto.Messages;

namespace Transacto.Domain {
	public class ChartOfAccounts : AggregateRoot {
		public const string Identifier = "chartOfAccounts";
		public static readonly Func<ChartOfAccounts> Factory = () => new ChartOfAccounts();

		public override string Id { get; } = Identifier;

		private State _state;

		public Account this[AccountNumber accountNumber] =>
			_state.AccountNames.TryGetValue(accountNumber, out var accountName)
				? Account.For(accountName, accountNumber)
				: Account.For(default, accountNumber);

		private ChartOfAccounts() {
			_state = new State();
		}

		protected override void ApplyEvent(object _) => _state = _ switch {
			AccountDefined e => _state with {
				AccountNumbers = _state.AccountNumbers.Add(new AccountNumber(e.AccountNumber)),
				AccountNames = _state.AccountNames.Add(new AccountNumber(e.AccountNumber),
					new AccountName(e.AccountName))
			},
			AccountDeactivated e => _state with {
				AccountNumbers = _state.AccountNumbers.Remove(new AccountNumber(e.AccountNumber)),
				DeactivatedAccountNumbers = _state.DeactivatedAccountNumbers.Add(new AccountNumber(e.AccountNumber))
			},
			AccountReactivated e => _state with {
				DeactivatedAccountNumbers = _state.DeactivatedAccountNumbers.Remove(new AccountNumber(e.AccountNumber)),
				AccountNumbers = _state.AccountNumbers.Add(new AccountNumber(e.AccountNumber))
			},
			AccountRenamed e => _state with {
				AccountNames = _state.AccountNames.SetItem(new AccountNumber(e.AccountNumber),
					new AccountName(e.NewAccountName))
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
			if (_state.AccountNames.ContainsKey(accountNumber)) {
				throw new AccountExistsException(accountNumber);
			}
		}

		private void MustContainAccountNumber(AccountNumber accountNumber) {
			if (!_state.AccountNames.ContainsKey(accountNumber)) {
				throw new AccountNotFoundException(accountNumber);
			}
		}

		private bool IsInactive(AccountNumber accountNumber) => _state.DeactivatedAccountNumbers.Contains(accountNumber);

		private bool IsActive(AccountNumber accountNumber) => _state.AccountNumbers.Contains(accountNumber);

		private record State {
			public ImmutableHashSet<AccountNumber> AccountNumbers { get; init; } =
				ImmutableHashSet<AccountNumber>.Empty;

			public ImmutableHashSet<AccountNumber> DeactivatedAccountNumbers { get; init; } =
				ImmutableHashSet<AccountNumber>.Empty;

			public ImmutableDictionary<AccountNumber, AccountName> AccountNames { get; init; } =
				ImmutableDictionary<AccountNumber, AccountName>.Empty;
		}
	}
}
