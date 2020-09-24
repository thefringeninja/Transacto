using System;
using System.Threading.Tasks;
using Transacto.Domain;
using Transacto.Messages;
using Transacto.Testing;
using Xunit;

namespace Transacto.Application {
    public class ChartOfAccountsTests {
        private readonly FactRecorder _facts;
        private readonly ChartOfAccountsHandlers _handler;

        public ChartOfAccountsTests() {
            _facts = new FactRecorder();
            _handler = new ChartOfAccountsHandlers(new ChartOfAccountsTestRepository(_facts));
        }

        [Theory, AutoTransactoData]
        public Task defining_an_account(AccountName accountName, AccountNumber accountNumber) =>
            new Scenario()
                .GivenNone()
                .When(new DefineAccount {
                    AccountName = accountName.ToString(),
                    AccountNumber = accountNumber.ToInt32()
                })
                .Then("chartOfAccounts", new AccountDefined {
                    AccountName = accountName.ToString(),
                    AccountNumber = accountNumber.ToInt32()
                })
                .Assert(_handler, _facts);

        [Theory, AutoTransactoData]
        public Task defining_a_second_account(AccountName accountName, AccountNumber accountNumber,
            AccountName secondAccountName, AccountNumber secondAccountNumber) =>
            new Scenario()
                .Given("chartOfAccounts", new AccountDefined {
                    AccountName = accountName.ToString(),
                    AccountNumber = accountNumber.ToInt32()
                })
                .When(new DefineAccount {
                    AccountName = secondAccountName.ToString(),
                    AccountNumber = secondAccountNumber.ToInt32()
                })
                .Then("chartOfAccounts", new AccountDefined {
                    AccountName = secondAccountName.ToString(),
                    AccountNumber = secondAccountNumber.ToInt32()
                })
                .Assert(_handler, _facts);

        [Theory, AutoTransactoData]
        public Task defining_the_same_account_throws(AccountName accountName, AccountNumber accountNumber) =>
            new Scenario()
                .Given("chartOfAccounts", new AccountDefined {
                    AccountName = accountName.ToString(),
                    AccountNumber = accountNumber.ToInt32()
                })
                .When(new DefineAccount {
                    AccountName = accountName.ToString(),
                    AccountNumber = accountNumber.ToInt32()
                })
                .Throws(new AccountExistsException(accountNumber))
                .Assert(_handler, _facts);

        [Theory, AutoTransactoData]
        public Task renaming_an_account(AccountName accountName, AccountNumber accountNumber,
            AccountName secondAccountName) =>
            new Scenario()
                .Given("chartOfAccounts", new AccountDefined {
                    AccountName = accountName.ToString(),
                    AccountNumber = accountNumber.ToInt32()
                })
                .When(new RenameAccount {
                    NewAccountName = secondAccountName.ToString(),
                    AccountNumber = accountNumber.ToInt32()
                })
                .Then("chartOfAccounts", new AccountRenamed {
                    NewAccountName = secondAccountName.ToString(),
                    AccountNumber = accountNumber.ToInt32()
                })
                .Assert(_handler, _facts);

        [Theory, AutoTransactoData]
        public Task deactivating_an_account(AccountName accountName, AccountNumber accountNumber) =>
            new Scenario()
                .Given("chartOfAccounts", new AccountDefined {
                    AccountName = accountName.ToString(),
                    AccountNumber = accountNumber.ToInt32()
                })
                .When(new DeactivateAccount {
                    AccountNumber = accountNumber.ToInt32()
                })
                .Then("chartOfAccounts", new AccountDeactivated {
                    AccountNumber = accountNumber.ToInt32()
                })
                .Assert(_handler, _facts);

        [Theory, AutoTransactoData]
        public Task reactivating_a_deactivated_account(AccountName accountName, AccountNumber accountNumber) =>
            new Scenario()
                .Given("chartOfAccounts", new AccountDefined {
                    AccountName = accountName.ToString(),
                    AccountNumber = accountNumber.ToInt32()
                }, new AccountDeactivated {
                    AccountNumber = accountNumber.ToInt32()
                })
                .When(new ReactivateAccount {
                    AccountNumber = accountNumber.ToInt32()
                })
                .Then("chartOfAccounts", new AccountReactivated {
                    AccountNumber = accountNumber.ToInt32()
                })
                .Assert(_handler, _facts);

        [Theory, AutoTransactoData]
        public Task reactivating_an_active_account(AccountName accountName, AccountNumber accountNumber) =>
            new Scenario()
                .Given("chartOfAccounts", new AccountDefined {
                    AccountName = accountName.ToString(),
                    AccountNumber = accountNumber.ToInt32()
                })
                .When(new ReactivateAccount {
                    AccountNumber = accountNumber.ToInt32()
                })
                .ThenNone()
                .Assert(_handler, _facts);

        [Theory, AutoTransactoData]
        public Task deactivating_a_deactivated_account(AccountName accountName, AccountNumber accountNumber) =>
            new Scenario()
                .Given("chartOfAccounts", new AccountDefined {
                    AccountName = accountName.ToString(),
                    AccountNumber = accountNumber.ToInt32()
                }, new AccountDeactivated {
                    AccountNumber = accountNumber.ToInt32()
                })
                .When(new DeactivateAccount {
                    AccountNumber = accountNumber.ToInt32()
                })
                .ThenNone()
                .Assert(_handler, _facts);

        [Theory, AutoTransactoData]
        public Task renaming_an_account_when_no_account_defined_throws(AccountName accountName,
            AccountNumber accountNumber) =>
            new Scenario()
                .GivenNone()
                .When(new RenameAccount {
                    NewAccountName = accountName.ToString(),
                    AccountNumber = accountNumber.ToInt32()
                })
                .Throws(new InvalidOperationException())
                .Assert(_handler, _facts);

        [Theory, AutoTransactoData]
        public Task renaming_an_account_when_it_was_not_defined_throws(AccountName accountName,
            AccountNumber accountNumber,
            AccountName secondAccountName, AccountNumber secondAccountNumber) =>
            new Scenario()
                .Given("chartOfAccounts", new AccountDefined {
                    AccountName = accountName.ToString(),
                    AccountNumber = accountNumber.ToInt32()
                })
                .When(new RenameAccount {
                    NewAccountName = secondAccountName.ToString(),
                    AccountNumber = secondAccountNumber.ToInt32()
                })
                .Throws(new AccountNotFoundException(secondAccountNumber))
                .Assert(_handler, _facts);

        [Theory, AutoTransactoData]
        public Task deactivating_an_account_when_no_account_defined_throws(AccountNumber accountNumber) =>
            new Scenario()
                .GivenNone()
                .When(new DeactivateAccount {
                    AccountNumber = accountNumber.ToInt32()
                })
                .Throws(new InvalidOperationException())
                .Assert(_handler, _facts);

        [Theory, AutoTransactoData]
        public Task deactivating_an_account_when_it_was_not_defined_throws(AccountName accountName,
            AccountNumber accountNumber, AccountNumber secondAccountNumber) =>
            new Scenario()
                .Given("chartOfAccounts", new AccountDefined {
                    AccountName = accountName.ToString(),
                    AccountNumber = accountNumber.ToInt32()
                })
                .When(new DeactivateAccount {
                    AccountNumber = secondAccountNumber.ToInt32()
                })
                .Throws(new AccountNotFoundException(secondAccountNumber))
                .Assert(_handler, _facts);

        [Theory, AutoTransactoData]
        public Task reactivating_an_account_when_no_account_defined_throws(AccountNumber accountNumber) =>
            new Scenario()
                .GivenNone()
                .When(new ReactivateAccount {
                    AccountNumber = accountNumber.ToInt32()
                })
                .Throws(new InvalidOperationException())
                .Assert(_handler, _facts);

        [Theory, AutoTransactoData]
        public Task reactivating_an_account_when_it_was_not_defined_throws(AccountName accountName,
            AccountNumber accountNumber, AccountNumber secondAccountNumber) =>
            new Scenario()
                .Given("chartOfAccounts", new AccountDefined {
                    AccountName = accountName.ToString(),
                    AccountNumber = accountNumber.ToInt32()
                })
                .When(new ReactivateAccount {
                    AccountNumber = secondAccountNumber.ToInt32()
                })
                .Throws(new AccountNotFoundException(secondAccountNumber))
                .Assert(_handler, _facts);
    }
}
