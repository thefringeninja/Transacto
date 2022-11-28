INSERT INTO purchase_orders(purchase_order_id,
							purchase_order_number,
							vendor_id)
VALUES (@purchase_order_id,
		@purchase_order_number,
		@vendor_id)
ON CONFLICT (purchase_order_id) DO NOTHING

