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

		private ChartOfAccounts() {
			_state = new State();
		}

		protected override void ApplyEvent(object _) => _state = _ switch {
			AccountDefined e => _state with {
				AccountNumbers = _state.AccountNumbers.Add(new AccountNumber(e.AccountNumber))
			},
			AccountDeactivated e => _state with {
				AccountNumbers = _state.AccountNumbers.Remove(new AccountNumber(e.AccountNumber)),
				DeactivatedAccountNumbers = _state.DeactivatedAccountNumbers.Add(new AccountNumber(e.AccountNumber))
			},
			AccountReactivated e => _state with {
				DeactivatedAccountNumbers = _state.DeactivatedAccountNumbers.Remove(new AccountNumber(e.AccountNumber)),
				AccountNumbers = _state.AccountNumbers.Add(new AccountNumber(e.AccountNumber))
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

			if (!IsActive(accountNumber)) {
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
			if (!IsActive(accountNumber) && !IsInactive(accountNumber)) {
				return;
			}

			throw new AccountExistsException(accountNumber);
		}

		private void MustContainAccountNumber(AccountNumber accountNumber) {
			if (IsActive(accountNumber) || IsInactive(accountNumber)) {
				return;
			}

			throw new AccountNotFoundException(accountNumber);
		}

		private bool IsInactive(AccountNumber accountNumber) => _state.DeactivatedAccountNumbers.Contains(accountNumber);

		private bool IsActive(AccountNumber accountNumber) => _state.AccountNumbers.Contains(accountNumber);

		private record State {
			public ImmutableHashSet<AccountNumber> AccountNumbers { get; init; } =
				ImmutableHashSet<AccountNumber>.Empty;

			public ImmutableHashSet<AccountNumber> DeactivatedAccountNumbers { get; init; } =
				ImmutableHashSet<AccountNumber>.Empty;
		}
	}
}
