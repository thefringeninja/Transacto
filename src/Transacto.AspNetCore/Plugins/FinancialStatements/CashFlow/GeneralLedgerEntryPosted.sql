REPLACE INTO cash_flow_daily (date, account_number, balance)
SELECT ut.date, uti.account_number, uti.amount + IFNULL(p.balance, 0)
FROM unposted_transactions AS ut
		 JOIN unposted_transactions_items AS uti
			  ON ut.general_ledger_entry_id = uti.general_ledger_entry_id
		 LEFT OUTER JOIN cash_flow_daily as p
						 ON p.date = ut.date AND p.account_number = uti.account_number
		 LEFT OUTER JOIN accounts as a
						 ON a.account_number = p.account_number
WHERE ut.general_ledger_entry_id = @general_ledger_entry_id
  AND (SELECT COUNT(*)
	   FROM unposted_transactions_items uti
				LEFT OUTER JOIN accounts as a
								ON uti.account_number = a.account_number
	   WHERE a.affects_cash = 1) > 0;

DELETE
FROM unposted_transactions
WHERE general_ledger_entry_id = @general_ledger_entry_id;

DELETE
FROM unposted_transactions_items
WHERE general_ledger_entry_id = @general_ledger_entry_id;
