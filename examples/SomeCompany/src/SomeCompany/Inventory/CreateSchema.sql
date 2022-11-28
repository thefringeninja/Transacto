CREATE TABLE IF NOT EXISTS inventory_ledger (
    inventory_item_id UUID NOT NULL,
    sku VARCHAR(256) NOT NULL,
    on_hand DECIMAL NOT NULL,
    on_order DECIMAL NOT NULL,
    committed DECIMAL NOT NULL,
    available DECIMAL GENERATED ALWAYS AS (on_hand - committed) STORED,
    CONSTRAINT pk_inventory_ledger PRIMARY KEY (inventory_item_id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_inventory_ledger_sku
ON inventory_ledger (sku);
