UPDATE inventory_ledger
SET on_order = on_order - @quantity,
	on_hand  = on_hand + @quantity
WHERE inventory_item_id = @inventory_item_id

