-- Migration: 004_transactions_and_cursor
-- Requires: 003_plaid_link_and_linked_banks.sql
-- Transactions from Plaid /transactions/sync; optional linked_bank_account_id; Plaid sync cursor on linked_banks;
-- plaid_finance_category_primary: Plaid PFC primary taxonomy for charts/filters (seeded like subscription_plans);
--   pfc_version is free-form text (v1, v2, future v3…) — not constrained to an enum.
--   join to transactions on (pfc_version, pfc_primary) = (pfc_version, code).
-- App lists transactions with OFFSET/LIMIT (numbered pagination), not this migration.

-- One cursor per Plaid Item (linked_banks row) for incremental /transactions/sync.
ALTER TABLE linked_banks
    ADD COLUMN IF NOT EXISTS plaid_transactions_cursor TEXT;

-- ---------------------------------------------------------------------------
-- plaid_finance_category_primary: static reference (same idea as subscription_plans)
-- Plaid field: personal_finance_category.primary. Codes from pfc-taxonomy-all.csv (unique per PFCv1 / PFCv2).
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS plaid_finance_category_primary (
    pfc_version     TEXT NOT NULL,
    code            TEXT NOT NULL,
    display_name    TEXT NOT NULL,
    sort_order      INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (pfc_version, code)
);

INSERT INTO plaid_finance_category_primary (pfc_version, code, display_name, sort_order) VALUES
    ('v1', 'BANK_FEES', 'Bank fees', 10),
    ('v1', 'ENTERTAINMENT', 'Entertainment', 20),
    ('v1', 'FOOD_AND_DRINK', 'Food & drink', 30),
    ('v1', 'GENERAL_MERCHANDISE', 'General merchandise', 40),
    ('v1', 'GENERAL_SERVICES', 'General services', 50),
    ('v1', 'GOVERNMENT_AND_NON_PROFIT', 'Government & nonprofit', 60),
    ('v1', 'HOME_IMPROVEMENT', 'Home improvement', 70),
    ('v1', 'INCOME', 'Income', 80),
    ('v1', 'LOAN_PAYMENTS', 'Loan payments', 90),
    ('v1', 'MEDICAL', 'Medical', 100),
    ('v1', 'PERSONAL_CARE', 'Personal care', 110),
    ('v1', 'RENT_AND_UTILITIES', 'Rent & utilities', 120),
    ('v1', 'TRANSFER_IN', 'Transfer in', 130),
    ('v1', 'TRANSFER_OUT', 'Transfer out', 140),
    ('v1', 'TRANSPORTATION', 'Transportation', 150),
    ('v1', 'TRAVEL', 'Travel', 160)
ON CONFLICT (pfc_version, code) DO NOTHING;

INSERT INTO plaid_finance_category_primary (pfc_version, code, display_name, sort_order) VALUES
    ('v2', 'BANK_FEES', 'Bank fees', 10),
    ('v2', 'ENTERTAINMENT', 'Entertainment', 20),
    ('v2', 'FOOD_AND_DRINK', 'Food & drink', 30),
    ('v2', 'GENERAL_MERCHANDISE', 'General merchandise', 40),
    ('v2', 'GENERAL_SERVICES', 'General services', 50),
    ('v2', 'GOVERNMENT_AND_NON_PROFIT', 'Government & nonprofit', 60),
    ('v2', 'HOME_IMPROVEMENT', 'Home improvement', 70),
    ('v2', 'INCOME', 'Income', 80),
    ('v2', 'LOAN_DISBURSEMENTS', 'Loan disbursements', 85),
    ('v2', 'LOAN_PAYMENTS', 'Loan payments', 90),
    ('v2', 'MEDICAL', 'Medical', 100),
    ('v2', 'OTHER', 'Other', 105),
    ('v2', 'PERSONAL_CARE', 'Personal care', 110),
    ('v2', 'RENT_AND_UTILITIES', 'Rent & utilities', 120),
    ('v2', 'TRANSFER_IN', 'Transfer in', 130),
    ('v2', 'TRANSFER_OUT', 'Transfer out', 140),
    ('v2', 'TRANSPORTATION', 'Transportation', 150),
    ('v2', 'TRAVEL', 'Travel', 160)
ON CONFLICT (pfc_version, code) DO NOTHING;

ALTER TABLE plaid_finance_category_primary ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Public read plaid finance category primary" ON plaid_finance_category_primary;
CREATE POLICY "Public read plaid finance category primary"
    ON plaid_finance_category_primary FOR SELECT
    USING (true);

CREATE INDEX IF NOT EXISTS idx_plaid_finance_category_primary_version_sort
    ON plaid_finance_category_primary(pfc_version, sort_order, code);

CREATE TABLE IF NOT EXISTS transactions (
    id                         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    profile_id                 UUID NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
    linked_bank_account_id     UUID REFERENCES linked_bank_accounts(id) ON DELETE SET NULL,
    plaid_transaction_id       TEXT NOT NULL,

    amount                     NUMERIC NOT NULL,
    iso_currency_code          TEXT,

    date                       DATE NOT NULL,
    authorized_date            DATE,
    authorized_datetime        TIMESTAMPTZ,

    name                       TEXT NOT NULL DEFAULT '',
    merchant_name              TEXT,
    merchant_entity_id         TEXT,

    pending                    BOOLEAN NOT NULL DEFAULT FALSE,
    pending_transaction_id     TEXT,

    payment_channel            TEXT,
    transaction_type           TEXT,

    pfc_primary                TEXT,
    pfc_detailed               TEXT,
    pfc_confidence_level       TEXT,
    pfc_version                TEXT,

    logo_url                   TEXT,
    website                    TEXT,

    status                     TEXT NOT NULL DEFAULT 'active'
        CHECK (status IN ('active', 'account_opted_out', 'soft_deleted')),

    removed_at                 TIMESTAMPTZ,

    created_at                 TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                 TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_transactions_profile_plaid_txn
        UNIQUE (profile_id, plaid_transaction_id)
);

ALTER TABLE transactions ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Users manage own transactions" ON transactions;
CREATE POLICY "Users manage own transactions"
    ON transactions FOR ALL
    USING (profile_id = auth.uid())
    WITH CHECK (profile_id = auth.uid());

DROP POLICY IF EXISTS "Admin read all transactions" ON transactions;
CREATE POLICY "Admin read all transactions"
    ON transactions FOR SELECT
    USING (auth.role() = 'admin');

CREATE INDEX IF NOT EXISTS idx_transactions_profile_id_date
    ON transactions(profile_id, date DESC)
    WHERE removed_at IS NULL;

CREATE INDEX IF NOT EXISTS idx_transactions_linked_bank_account_id_date
    ON transactions(linked_bank_account_id, date DESC)
    WHERE removed_at IS NULL AND linked_bank_account_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_transactions_pending
    ON transactions(linked_bank_account_id)
    WHERE pending = TRUE AND removed_at IS NULL AND linked_bank_account_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_transactions_profile_pfc_primary_date
    ON transactions(profile_id, pfc_primary, date DESC)
    WHERE removed_at IS NULL AND pfc_primary IS NOT NULL;
