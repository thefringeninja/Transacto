INSERT INTO unposted_transactions_items (general_ledger_entry_id, account_number, amount)
SELECT @general_ledger_entry_id, @account_number, @amount
WHERE NOT EXISTS (SELECT 1
				  FROM closing_transactions
				  WHERE closing_general_ledger_entry_id = @general_ledger_entry_id) 
