using System.Collections;
using System.Collections.Immutable;

namespace Transacto.Domain; 

internal class AccountBalanceCollection : IEnumerable<Account> {
	private ImmutableDictionary<AccountNumber, Account> _items;

	public AccountBalanceCollection() => _items = ImmutableDictionary<AccountNumber, Account>.Empty;

	public void AddOrUpdate(AccountNumber accountNumber, Func<Account> add, Func<Account, Account> update) =>
		_items = _items.SetItem(accountNumber, _items.TryGetValue(accountNumber, out var account)
			? update(account)
			: update(add()));

	public Money GetBalance() => _items.Values.Select(account => account switch {
		ExpenseAccount or AssetAccount => account.Balance,
		_ => -account.Balance
	}).Sum();

	public IEnumerator<Account> GetEnumerator() =>
		_items.Values.OrderBy(account => account.AccountNumber.ToInt32()).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
