UPDATE __schema__.inventory_ledger
SET on_order = on_order + @quantity
WHERE inventory_item_id = @inventory_item_id

