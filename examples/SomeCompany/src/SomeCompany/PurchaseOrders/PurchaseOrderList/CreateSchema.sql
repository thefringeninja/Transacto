CREATE TABLE IF NOT EXISTS purchase_order_list
(
	purchase_order_id     UUID NOT NULL,
	purchase_order_number INT  NOT NULL,
	vendor_id             UUID NOT NULL,
	CONSTRAINT pk_purchase_order_list PRIMARY KEY (purchase_order_id)
);
