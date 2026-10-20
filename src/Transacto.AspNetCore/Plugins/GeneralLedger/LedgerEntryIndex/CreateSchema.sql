CREATE TABLE IF NOT EXISTS ledger_entry_index(
	general_ledger_entry_id BLOB PRIMARY KEY,
	reference_number TEXT,
	created_on TEXT
);

CREATE INDEX IF NOT EXISTS idx_ledger_entry_index_reference_number
ON ledger_entry_index (reference_number);
