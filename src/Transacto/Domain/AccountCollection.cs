using System.Collections;
using System.Collections.Immutable;

namespace Transacto.Domain; 

internal class AccountCollection : IEnumerable<Account> {
	private ImmutableDictionary<AccountNumber, Account> _items;

	public AccountCollection() {
		_items = ImmutableDictionary<AccountNumber, Account>.Empty;
	}

	public void AddOrUpdate(AccountNumber accountNumber, Func<Account> add, Func<Account, Account> update) =>
		_items = _items.SetItem(accountNumber, _items.TryGetValue(accountNumber, out var account)
			? update(account)
			: update(add()));

	public IEnumerator<Account> GetEnumerator() => _items.Values.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
