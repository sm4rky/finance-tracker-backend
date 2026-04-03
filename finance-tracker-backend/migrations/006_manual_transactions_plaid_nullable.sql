-- Manual transactions: no Plaid transaction id. PostgreSQL allows multiple NULLs in UNIQUE (profile_id, plaid_transaction_id).
ALTER TABLE transactions
    ALTER COLUMN plaid_transaction_id DROP NOT NULL;
