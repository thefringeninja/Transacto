CREATE TABLE IF NOT EXISTS __schema__.balance_sheet_report
(
    period_month   SMALLINT NOT NULL,
    period_year    SMALLINT NOT NULL,
    account_number SMALLINT NOT NULL,
    balance        MONEY    NOT NULL,
    CONSTRAINT pk_balance_sheet_report PRIMARY KEY (period_month, period_year, account_number)
);

CREATE TABLE IF NOT EXISTS __schema__.balance_sheet_items_unposted
(
    general_ledger_entry_id UUID     NOT NULL,
    account_number          SMALLINT NOT NULL,
    debit                   MONEY    NOT NULL DEFAULT 0,
    credit                  MONEY    NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS __schema__.balance_sheet_general_ledger_entry_period
(
    general_ledger_entry_id UUID     NOT NULL,
    period_year             SMALLINT NOT NULL,
    period_month            SMALLINT NOT NULL,
    CONSTRAINT pk_balance_sheet_general_ledger_entry_period PRIMARY KEY (general_ledger_entry_id)
);
