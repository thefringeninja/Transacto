using Transacto.Domain;
using Transacto.Testing;
using static Transacto.ChartOfAccountsSpecifications;

namespace Transacto.Application;

public class ChartOfAccountsTests {
	private readonly FactRecorder _facts;
	private readonly ChartOfAccountsHandlers _handler;

	public ChartOfAccountsTests() {
		_facts = new FactRecorder();
		_handler = new ChartOfAccountsHandlers(new ChartOfAccountsTestRepository(_facts));
	}

	[AutoFixtureData]
	public Task configuring_the_chart_of_accounts(int accountNumberWidth) =>
		new ConfiguringTheChartOfAccounts(
				accountNumberWidth %
				(ChartOfAccountsSettings.MaxAccountNumberWidth - ChartOfAccountsSettings.MinAccountNumberWidth + 1) +
				ChartOfAccountsSettings.MinAccountNumberWidth)
			.Assert(_handler, _facts);

	[AutoFixtureData]
	public Task configuring_the_chart_of_accounts_more_than_once(int accountNumberWidth) =>
		new ConfiguringTheChartOfAccountsMoreThanOnce(
				accountNumberWidth %
				(ChartOfAccountsSettings.MaxAccountNumberWidth - ChartOfAccountsSettings.MinAccountNumberWidth + 1) +
				ChartOfAccountsSettings.MinAccountNumberWidth)
			.Assert(_handler, _facts);

	[AutoFixtureData]
	public Task defining_an_account_before_setup(AccountName accountName, AccountNumber accountNumber) =>
		new DefiningAnAccountBeforeConfiguration(Account.For(accountNumber, accountName)).Assert(_handler, _facts);

	[AutoFixtureData]
	public Task defining_an_account(AccountName accountName, AccountNumber accountNumber) =>
		new DefiningAnAccount(Account.For(accountNumber, accountName)).Assert(_handler, _facts);

	[AutoFixtureData]
	public Task defining_an_account_when_account_number_wrong_length_trows(AccountName accountName,
		AccountNumber accountNumber) =>
		new DefiningAnAccountWhenAccountNumberWrongLengthThrows(Account.For(accountNumber, accountName),
			nameof(accountNumber)).Assert(_handler, _facts);

	[AutoFixtureData]
	public Task defining_a_second_account(AccountName accountName, AccountNumber accountNumber,
		AccountName secondAccountName, AccountNumber secondAccountNumber) =>
		new DefiningASecondAccount(Account.For(accountNumber, accountName),
			Account.For(secondAccountNumber, secondAccountName)).Assert(_handler, _facts);


	[AutoFixtureData]
	public Task defining_the_same_account_throws(AccountName accountName, AccountNumber accountNumber) =>
		new DefiningTheSameAccountThrows(Account.For(accountNumber, accountName)).Assert(_handler, _facts);

	[AutoFixtureData]
	public Task renaming_an_account(AccountName accountName, AccountNumber accountNumber,
		AccountName secondAccountName) =>
		new RenamingAnAccount(Account.For(accountNumber, accountName), secondAccountName)
			.Assert(_handler, _facts);

	[AutoFixtureData]
	public Task deactivating_an_account(AccountName accountName, AccountNumber accountNumber) =>
		new DeactivatingAnAccount(Account.For(accountNumber, accountName))
			.Assert(_handler, _facts);

	[AutoFixtureData]
	public Task reactivating_a_deactivated_account(AccountName accountName, AccountNumber accountNumber) =>
		new ReactivatingADeactivatedAccount(Account.For(accountNumber, accountName))
			.Assert(_handler, _facts);

	[AutoFixtureData]
	public Task reactivating_an_active_account(AccountName accountName, AccountNumber accountNumber) =>
		new ReactivatingAnActiveAccount(Account.For(accountNumber, accountName))
			.Assert(_handler, _facts);

	[AutoFixtureData]
	public Task deactivating_a_deactivated_account(AccountName accountName, AccountNumber accountNumber) =>
		new DeactivatingADeactivatedAccount(Account.For(accountNumber, accountName)).Assert(_handler, _facts);

	[AutoFixtureData]
	public Task renaming_an_account_when_no_account_defined_throws(AccountName accountName,
		AccountNumber accountNumber) =>
		new RenamingAnAccountWhenNoAccountDefinedThrows(Account.For(accountNumber, accountName))
			.Assert(_handler, _facts);

	[AutoFixtureData]
	public Task renaming_an_account_when_it_was_not_defined_throws(AccountName accountName, AccountNumber accountNumber,
		AccountName secondAccountName, AccountNumber secondAccountNumber) =>
		new RenamingAnAccountWhenItWasNotDefinedThrows(Account.For(accountNumber, accountName),
			Account.For(secondAccountNumber, secondAccountName)).Assert(_handler, _facts);

	[AutoFixtureData]
	public Task deactivating_an_account_when_no_account_defined_throws(AccountNumber accountNumber) =>
		new DeactivatingAnAccountWhenNoAccountDefinedThrows(accountNumber).Assert(_handler, _facts);

	[AutoFixtureData]
	public Task deactivating_an_account_when_it_was_not_defined_throws(AccountName accountName,
		AccountNumber accountNumber, AccountNumber secondAccountNumber) =>
		new DeactivatingAnAccountWhenItWasNotDefinedThrows(Account.For(accountNumber, accountName), secondAccountNumber)
			.Assert(_handler, _facts);

	[AutoFixtureData]
	public Task reactivating_an_account_when_no_account_defined_throws(AccountNumber accountNumber) =>
		new ReactivatingAnAccountWhenNoAccountDefinedThrows(accountNumber).Assert(_handler, _facts);

	[AutoFixtureData]
	public Task reactivating_an_account_when_it_was_not_defined_throws(AccountName accountName,
		AccountNumber accountNumber, AccountNumber secondAccountNumber) =>
		new ReactivatingAnAccountWhenItWasNotDefinedThrows(Account.For(accountNumber, accountName), secondAccountNumber)
			.Assert(_handler, _facts);
}
