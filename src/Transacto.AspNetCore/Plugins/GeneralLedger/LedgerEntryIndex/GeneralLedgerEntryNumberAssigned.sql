UPDATE ledger_entry_index
SET reference_number = @reference_number
WHERE general_ledger_entry_id = @general_ledger_entry_id
