REPLACE INTO profit_and_loss_daily (date, account_number, balance)
SELECT ut.date, uti.account_number, uti.amount + ifnull(p.balance, 0)
FROM unposted_transactions AS ut
		 JOIN unposted_transactions_items AS uti
			  ON ut.general_ledger_entry_id = uti.general_ledger_entry_id
		 LEFT OUTER JOIN profit_and_loss_daily as p
			  ON p.date = ut.date AND p.account_number = uti.account_number
WHERE ut.general_ledger_entry_id = @general_ledger_entry_id;

DELETE
FROM unposted_transactions
WHERE general_ledger_entry_id = @general_ledger_entry_id;

DELETE
FROM unposted_transactions_items
WHERE general_ledger_entry_id = @general_ledger_entry_id;
