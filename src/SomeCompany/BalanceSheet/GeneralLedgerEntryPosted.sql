BEGIN TRANSACTION;

INSERT INTO __schema__.balance_sheet_report
    (account_number, period_year, period_month, balance)
SELECT u.account_number, p.period_year, p.period_month, SUM(u.debit - u.credit) as balance
FROM __schema__.balance_sheet_general_ledger_entry_period p
         JOIN __schema__.balance_sheet_items_unposted u ON u.general_ledger_entry_id = p.general_ledger_entry_id
GROUP BY u.account_number, p.period_year, p.period_month
ON CONFLICT (account_number, period_year, period_month) DO UPDATE SET balance = excluded.balance + __schema__.balance_sheet_report.balance;

DELETE
FROM __schema__.balance_sheet_items_unposted
WHERE general_ledger_entry_id = @general_ledger_entry_id;

DELETE
FROM __schema__.balance_sheet_general_ledger_entry_period
WHERE general_ledger_entry_id = @general_ledger_entry_id;
COMMIT TRANSACTION;
