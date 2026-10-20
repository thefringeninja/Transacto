CREATE TABLE IF NOT EXISTS cash_flow_daily
(
	date           TEXT    NOT NULL,
	account_number INTEGER NOT NULL,
	balance        INTEGER NOT NULL,
	PRIMARY KEY (date, account_number)
);

CREATE TABLE IF NOT EXISTS accounts
(
	account_number     INTEGER NOT NULL,
	account_name       TEXT    NOT NULL,
	cash_flow_category INTEGER NOT NULL,
	affects_cash	   INTEGER NOT NULL,
	PRIMARY KEY (account_number)
);

CREATE TABLE IF NOT EXISTS unposted_transactions
(
	general_ledger_entry_id BLOB NOT NULL,
	date                    TEXT NOT NULL,
	PRIMARY KEY (general_ledger_entry_id)
);

CREATE TABLE IF NOT EXISTS unposted_transactions_items
(
	general_ledger_entry_id BLOB    NOT NULL,
	account_number          INTEGER NOT NULL,
	amount                  INTEGER NOT NULL,
	id                      INTEGER PRIMARY KEY
);

CREATE INDEX IF NOT EXISTS idx_unposted_transactions
	ON unposted_transactions_items (general_ledger_entry_id);

CREATE TABLE IF NOT EXISTS closing_transactions
(
	closing_general_ledger_entry_id BLOB NOT NULL,
	PRIMARY KEY (closing_general_ledger_entry_id)
)
