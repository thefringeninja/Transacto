using Transacto.Domain;
using Transacto.Messages;
using Transacto.Testing;

namespace Transacto;

public static class ChartOfAccountsSpecifications {
	public class DefiningAnAccount(Account account) : EventSpecification(new Scenario()
		.GivenNone()
		.When(new DefineAccount {
			AccountName = account.AccountName.ToString(),
			AccountNumber = account.AccountNumber.ToInt32()
		})
		.Then(ChartOfAccounts.Identifier, new AccountDefined {
			AccountName = account.AccountName.ToString(),
			AccountNumber = account.AccountNumber.ToInt32()
		}));

	public class DefiningASecondAccount(Account account, Account secondAccount) : EventSpecification(new Scenario()
		.Given(ChartOfAccounts.Identifier, new AccountDefined {
			AccountName = account.AccountName.ToString(),
			AccountNumber = account.AccountNumber.ToInt32()
		})
		.When(new DefineAccount {
			AccountName = secondAccount.AccountName.ToString(),
			AccountNumber = secondAccount.AccountNumber.ToInt32()
		})
		.Then(ChartOfAccounts.Identifier, new AccountDefined {
			AccountName = secondAccount.AccountName.ToString(),
			AccountNumber = secondAccount.AccountNumber.ToInt32()
		}));

	public class DefiningTheSameAccountThrows(AccountName accountName, AccountNumber accountNumber)
		: ExceptionSpecification(new Scenario()
			.Given(ChartOfAccounts.Identifier, new AccountDefined {
				AccountName = accountName.ToString(),
				AccountNumber = accountNumber.ToInt32()
			})
			.When(new DefineAccount {
				AccountName = accountName.ToString(),
				AccountNumber = accountNumber.ToInt32()
			})
			.Throws(new AccountExistsException(accountNumber)));

	public class RenamingAnAccount(Account account, AccountName secondAccountName) : EventSpecification(new Scenario()
		.Given(ChartOfAccounts.Identifier, new AccountDefined {
			AccountName = account.AccountName.ToString(),
			AccountNumber = account.AccountNumber.ToInt32()
		})
		.When(new RenameAccount {
			NewAccountName = secondAccountName.ToString(),
			AccountNumber = account.AccountNumber.ToInt32()
		})
		.Then(ChartOfAccounts.Identifier, new AccountRenamed {
			NewAccountName = secondAccountName.ToString(),
			AccountNumber = account.AccountNumber.ToInt32()
		}));

	public class DeactivatingAnAccount(Account account) : EventSpecification(new Scenario()
		.Given(ChartOfAccounts.Identifier, new AccountDefined {
			AccountName = account.AccountName.ToString(),
			AccountNumber = account.AccountNumber.ToInt32()
		})
		.When(new DeactivateAccount {
			AccountNumber = account.AccountNumber.ToInt32()
		})
		.Then(ChartOfAccounts.Identifier, new AccountDeactivated {
			AccountNumber = account.AccountNumber.ToInt32()
		}));

	public class ReactivatingADeactivatedAccount(Account account) : EventSpecification(new Scenario()
		.Given(ChartOfAccounts.Identifier, new AccountDefined {
			AccountName = account.AccountName.ToString(),
			AccountNumber = account.AccountNumber.ToInt32()
		}, new AccountDeactivated {
			AccountNumber = account.AccountNumber.ToInt32()
		})
		.When(new ReactivateAccount {
			AccountNumber = account.AccountNumber.ToInt32()
		})
		.Then(ChartOfAccounts.Identifier, new AccountReactivated {
			AccountNumber = account.AccountNumber.ToInt32()
		}));

	public class ReactivatingAnActiveAccount(Account account) : EventSpecification(new Scenario()
		.Given(ChartOfAccounts.Identifier, new AccountDefined {
			AccountName = account.AccountName.ToString(),
			AccountNumber = account.AccountNumber.ToInt32()
		})
		.When(new ReactivateAccount {
			AccountNumber = account.AccountNumber.ToInt32()
		})
		.ThenNone());

	public class DeactivatingADeactivatedAccount(Account account) : EventSpecification(new Scenario()
		.Given(ChartOfAccounts.Identifier, new AccountDefined {
			AccountName = account.AccountName.ToString(),
			AccountNumber = account.AccountNumber.ToInt32()
		}, new AccountDeactivated {
			AccountNumber = account.AccountNumber.ToInt32()
		})
		.When(new DeactivateAccount {
			AccountNumber = account.AccountNumber.ToInt32()
		})
		.ThenNone());

	public class RenamingAnAccountWhenNoAccountDefinedThrows(Account account)
		: ExceptionSpecification(new Scenario()
			.GivenNone()
			.When(new RenameAccount {
				NewAccountName = account.AccountName.ToString(),
				AccountNumber = account.AccountNumber.ToInt32()
			})
			.Throws(new ChartOfAccountsNotFoundException()));

	public class RenamingAnAccountWhenItWasNotDefinedThrows(Account account, Account secondAccount)
		: ExceptionSpecification(new Scenario()
			.Given(ChartOfAccounts.Identifier, new AccountDefined {
				AccountName = account.AccountName.ToString(),
				AccountNumber = account.AccountNumber.ToInt32()
			})
			.When(new RenameAccount {
				NewAccountName = secondAccount.AccountName.ToString(),
				AccountNumber = secondAccount.AccountNumber.ToInt32()
			})
			.Throws(new AccountNotFoundException(secondAccount.AccountNumber)));

	public class DeactivatingAnAccountWhenNoAccountDefinedThrows(AccountNumber accountNumber)
		: ExceptionSpecification(new Scenario()
			.GivenNone()
			.When(new DeactivateAccount {
				AccountNumber = accountNumber.ToInt32()
			})
			.Throws(new ChartOfAccountsNotFoundException()));

	public class DeactivatingAnAccountWhenItWasNotDefinedThrows(
		Account account,
		AccountNumber secondAccountNumber) : ExceptionSpecification(new Scenario()
		.Given(ChartOfAccounts.Identifier, new AccountDefined {
			AccountName = account.AccountName.ToString(),
			AccountNumber = account.AccountNumber.ToInt32()
		})
		.When(new DeactivateAccount {
			AccountNumber = secondAccountNumber.ToInt32()
		})
		.Throws(new AccountNotFoundException(secondAccountNumber)));

	public class ReactivatingAnAccountWhenNoAccountDefinedThrows(AccountNumber accountNumber)
		: ExceptionSpecification(new Scenario()
			.GivenNone()
			.When(new ReactivateAccount {
				AccountNumber = accountNumber.ToInt32()
			})
			.Throws(new ChartOfAccountsNotFoundException()));

	public class ReactivatingAnAccountWhenItWasNotDefinedThrows(
		Account account,
		AccountNumber secondAccountNumber) : ExceptionSpecification(new Scenario()
		.Given(ChartOfAccounts.Identifier, new AccountDefined {
			AccountName = account.AccountName.ToString(),
			AccountNumber = account.AccountNumber.ToInt32()
		})
		.When(new ReactivateAccount {
			AccountNumber = secondAccountNumber.ToInt32()
		})
		.Throws(new AccountNotFoundException(secondAccountNumber))
	);
}
