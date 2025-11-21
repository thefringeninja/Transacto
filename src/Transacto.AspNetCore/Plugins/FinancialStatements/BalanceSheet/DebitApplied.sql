INSERT INTO unposted_transactions_items (general_ledger_entry_id, account_number, amount)
SELECT @general_ledger_entry_id, @account_number, @amount
