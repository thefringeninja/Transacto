CREATE TABLE IF NOT EXISTS __schema__.purchase_orders
(
    purchase_order_id     UUID NOT NULL,
    purchase_order_number INT  NOT NULL,
    vendor_id             UUID NOT NULL,
    CONSTRAINT pk_balance_sheet_report PRIMARY KEY (purchase_order_id)
);

CREATE TABLE IF NOT EXISTS __schema__.purchase_order_items
(
    purchase_order_id UUID  NOT NULL,
    inventory_item_id UUID  NOT NULL,
    quantity          MONEY NOT NULL,
    unit_price        MONEY NOT NULL
);
