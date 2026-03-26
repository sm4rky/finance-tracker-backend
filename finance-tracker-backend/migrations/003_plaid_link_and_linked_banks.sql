-- Migration: 003_plaid_link_and_linked_banks
-- Requires: 001_profiles_and_subscriptions.sql
-- Plaid Link pending sessions, linked items (Plaid Item), and accounts.

-- ---------------------------------------------------------------------------
-- plaid_link_sessions: short-lived pending Link; at most one row per profile
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS plaid_link_sessions (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    profile_id      UUID NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
    link_token      TEXT NOT NULL,
    expires_at      TIMESTAMPTZ NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_plaid_link_sessions_profile_id UNIQUE (profile_id)
);

ALTER TABLE plaid_link_sessions ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Users manage own plaid link sessions" ON plaid_link_sessions;
CREATE POLICY "Users manage own plaid link sessions"
    ON plaid_link_sessions FOR ALL
    USING (profile_id = auth.uid())
    WITH CHECK (profile_id = auth.uid());

DROP POLICY IF EXISTS "Admin read all plaid link sessions" ON plaid_link_sessions;
CREATE POLICY "Admin read all plaid link sessions"
    ON plaid_link_sessions FOR SELECT
    USING (auth.role() = 'admin');

CREATE INDEX IF NOT EXISTS idx_plaid_link_sessions_profile_id ON plaid_link_sessions(profile_id);
CREATE INDEX IF NOT EXISTS idx_plaid_link_sessions_expires_at ON plaid_link_sessions(expires_at);

-- ---------------------------------------------------------------------------
-- linked_banks: Plaid Item / institution connection per profile (lifecycle here)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS linked_banks (
    id                           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    profile_id                   UUID NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
    plaid_item_id                TEXT NOT NULL,
    plaid_access_token_encrypted TEXT,
    institution_id               TEXT,
    institution_name             TEXT,
    status                       TEXT NOT NULL
        CHECK (status IN ('active', 'disconnected', 'relink_required', 'soft_deleted')),
    token_removed_at             TIMESTAMPTZ,
    disconnected_at              TIMESTAMPTZ,
    last_synced_at               TIMESTAMPTZ,
    created_at                   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

ALTER TABLE linked_banks ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Users manage own linked banks" ON linked_banks;
CREATE POLICY "Users manage own linked banks"
    ON linked_banks FOR ALL
    USING (profile_id = auth.uid())
    WITH CHECK (profile_id = auth.uid());

DROP POLICY IF EXISTS "Admin read all linked banks" ON linked_banks;
CREATE POLICY "Admin read all linked banks"
    ON linked_banks FOR SELECT
    USING (auth.role() = 'admin');

CREATE INDEX IF NOT EXISTS idx_linked_banks_profile_id ON linked_banks(profile_id);
CREATE INDEX IF NOT EXISTS idx_linked_banks_plaid_item_id ON linked_banks(plaid_item_id);
CREATE INDEX IF NOT EXISTS idx_linked_banks_profile_active
    ON linked_banks(profile_id)
    WHERE status <> 'soft_deleted';

CREATE UNIQUE INDEX IF NOT EXISTS uq_linked_banks_profile_plaid_item_active
    ON linked_banks(profile_id, plaid_item_id)
    WHERE status <> 'soft_deleted';

-- ---------------------------------------------------------------------------
-- linked_bank_accounts: accounts under one Plaid Item (balances snapshot; no account-level opt-out in MVP)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS linked_bank_accounts (
    id                         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    linked_bank_id             UUID NOT NULL REFERENCES linked_banks(id) ON DELETE CASCADE,
    plaid_account_id           TEXT NOT NULL,
    account_name               TEXT,
    official_name              TEXT,
    mask                       TEXT,
    type                       TEXT,
    subtype                    TEXT,

    current_balance            NUMERIC,
    available_balance          NUMERIC,
    limit_amount               NUMERIC,
    iso_currency_code          TEXT,
    unofficial_currency_code   TEXT,
    balance_last_fetched_at    TIMESTAMPTZ,

    is_active                  BOOLEAN NOT NULL DEFAULT TRUE,

    created_at                 TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                 TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

ALTER TABLE linked_bank_accounts ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Users manage own linked bank accounts" ON linked_bank_accounts;
CREATE POLICY "Users manage own linked bank accounts"
    ON linked_bank_accounts FOR ALL
    USING (
        EXISTS (
            SELECT 1 FROM linked_banks lb
            WHERE lb.id = linked_bank_accounts.linked_bank_id
              AND lb.profile_id = auth.uid()
        )
    )
    WITH CHECK (
        EXISTS (
            SELECT 1 FROM linked_banks lb
            WHERE lb.id = linked_bank_accounts.linked_bank_id
              AND lb.profile_id = auth.uid()
        )
    );

DROP POLICY IF EXISTS "Admin read all linked bank accounts" ON linked_bank_accounts;
CREATE POLICY "Admin read all linked bank accounts"
    ON linked_bank_accounts FOR SELECT
    USING (auth.role() = 'admin');

CREATE INDEX IF NOT EXISTS idx_linked_bank_accounts_linked_bank_id ON linked_bank_accounts(linked_bank_id);
CREATE INDEX IF NOT EXISTS idx_linked_bank_accounts_plaid_account_id ON linked_bank_accounts(plaid_account_id);

CREATE UNIQUE INDEX IF NOT EXISTS uq_linked_bank_accounts_bank_plaid_account
    ON linked_bank_accounts(linked_bank_id, plaid_account_id);
