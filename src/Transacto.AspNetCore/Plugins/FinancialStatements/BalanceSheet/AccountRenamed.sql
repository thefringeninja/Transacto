UPDATE account_names
SET account_name = @new_account_name
WHERE account_number = @account_number;
