INSERT INTO inventory_ledger (inventory_item_id, sku, on_hand, on_order, committed)
VALUES (@inventory_item_id, @sku, 0, 0, 0)
