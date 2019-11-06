using System;
using System.Collections.Generic;
using Transacto.Framework;
using Transacto.Messages;

namespace Transacto.Domain {
    public class ChartOfAccounts : AggregateRoot {
        public static readonly Func<ChartOfAccounts> Factory = () => new ChartOfAccounts();

        private readonly HashSet<AccountNumber> _accountNumbers;

        private ChartOfAccounts() {
            _accountNumbers = new HashSet<AccountNumber>();
            Register<AccountDefined>(e => _accountNumbers.Add(new AccountNumber(e.AccountNumber)));
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

            Apply(new AccountDeactivated {
                AccountNumber = accountNumber.ToInt32()
            });
        }

        public void ReactivateAccount(AccountNumber accountNumber) {
            MustContainAccountNumber(accountNumber);

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
            if (!_accountNumbers.Contains(accountNumber)) {
                return;
            }

            throw new InvalidOperationException();
        }

        private void MustContainAccountNumber(AccountNumber accountNumber) {
            if (_accountNumbers.Contains(accountNumber)) {
                return;
            }

            throw new InvalidOperationException();
        }
    }
}
