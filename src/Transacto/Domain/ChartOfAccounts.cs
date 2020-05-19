using System;
using System.Collections.Generic;
using Transacto.Framework;
using Transacto.Messages;

namespace Transacto.Domain {
	public class ChartOfAccounts : AggregateRoot {
		public static readonly Func<ChartOfAccounts> Factory = () => new ChartOfAccounts();

		private readonly HashSet<AccountNumber> _accountNumbers;
		private readonly HashSet<AccountNumber> _deactivatedAccountNumbers;

		public override string Id { get; } = "chartOfAccounts";

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

		public void MustNotBeDeactivated(AccountNumber accountNumber) {
			if (_deactivatedAccountNumbers.Contains(accountNumber)) {
				throw new InvalidOperationException();
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
			if (!IsActive(accountNumber) && !IsUnactive(accountNumber)) {
				return;
			}

			throw new InvalidOperationException();
		}

		private void MustContainAccountNumber(AccountNumber accountNumber) {
			if (IsActive(accountNumber) || IsUnactive(accountNumber)) {
				return;
			}

			throw new InvalidOperationException();
		}

		private bool IsUnactive(AccountNumber accountNumber) => _deactivatedAccountNumbers.Contains(accountNumber);

		private bool IsActive(AccountNumber accountNumber) => _accountNumbers.Contains(accountNumber);
	}
}
