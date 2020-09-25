using System;
using System.Collections.Generic;
using Transacto.Framework;
using Transacto.Messages;

namespace Transacto.Domain {
	public class ChartOfAccounts : AggregateRoot {
		public const string Identifier = "chartOfAccounts";
		public static readonly Func<ChartOfAccounts> Factory = () => new ChartOfAccounts();

		private readonly HashSet<AccountNumber> _accountNumbers;
		private readonly HashSet<AccountNumber> _deactivatedAccountNumbers;

		public override string Id { get; } = Identifier;

		private ChartOfAccounts() {
			_accountNumbers = new HashSet<AccountNumber>();
			_deactivatedAccountNumbers = new HashSet<AccountNumber>();
			Register<AccountDefined>(e => _accountNumbers.Add(new AccountNumber(e.AccountNumber)));
			Register<AccountDeactivated>(e => {
				var accountNumber = new AccountNumber(e.AccountNumber);
				_accountNumbers.Remove(accountNumber);
				_deactivatedAccountNumbers.Add(accountNumber);
			});
			Register<AccountReactivated>(e => {
				var accountNumber = new AccountNumber(e.AccountNumber);
				_accountNumbers.Add(accountNumber);
				_deactivatedAccountNumbers.Remove(accountNumber);
			});
		}

		public bool IsDeactivated(AccountNumber accountNumber) => _deactivatedAccountNumbers.Contains(accountNumber);

		public void MustNotBeDeactivated(AccountNumber accountNumber) {
			if (_deactivatedAccountNumbers.Contains(accountNumber)) {
				throw new AccountDeactivatedException(accountNumber);
			}
		}

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

		private bool IsInactive(AccountNumber accountNumber) => _deactivatedAccountNumbers.Contains(accountNumber);

		private bool IsActive(AccountNumber accountNumber) => _accountNumbers.Contains(accountNumber);
	}
}
