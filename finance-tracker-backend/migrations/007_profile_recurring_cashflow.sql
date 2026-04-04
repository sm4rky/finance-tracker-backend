-- Recurring cashflows: Plaid /transactions/recurring/get + manual rows. Server uses service_role; RLS for direct Supabase clients.

CREATE TABLE IF NOT EXISTS profile_recurring_cashflow (
    id                         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    profile_id                 UUID NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,

    plaid_stream_id            TEXT,
    linked_bank_account_id     UUID REFERENCES linked_bank_accounts(id) ON DELETE SET NULL,

    direction                  TEXT NOT NULL
        CHECK (direction IN ('inflow', 'outflow')),

    merchant_name              TEXT,
    description                TEXT,

    pfc_primary                TEXT,
    pfc_detailed               TEXT,

    frequency                  TEXT NOT NULL
        CHECK (frequency IN (
            'UNKNOWN',
            'WEEKLY',
            'BIWEEKLY',
            'SEMI_MONTHLY',
            'MONTHLY',
            'ANNUALLY',
            'ONE_TIME'
        )),

    last_amount                NUMERIC(18, 2),
    expected_amount            NUMERIC(18, 2) NOT NULL,
    expected_amount_user_set   BOOLEAN NOT NULL DEFAULT FALSE,

    first_date                 DATE,
    last_date                  DATE,
    predicted_next_date        DATE,

    status                     TEXT NOT NULL
        CHECK (status IN ('active', 'inactive', 'unlinked')),

    created_at                 TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                 TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_profile_recurring_cashflow_profile_plaid_stream_id
    ON profile_recurring_cashflow (profile_id, plaid_stream_id)
    WHERE plaid_stream_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_profile_recurring_cashflow_profile_id
    ON profile_recurring_cashflow (profile_id);

CREATE INDEX IF NOT EXISTS ix_profile_recurring_cashflow_linked_bank_account_id
    ON profile_recurring_cashflow (linked_bank_account_id);

CREATE INDEX IF NOT EXISTS ix_profile_recurring_cashflow_profile_status
    ON profile_recurring_cashflow (profile_id, status);

ALTER TABLE profile_recurring_cashflow ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Users manage own profile recurring cashflow" ON profile_recurring_cashflow;
CREATE POLICY "Users manage own profile recurring cashflow"
    ON profile_recurring_cashflow FOR ALL
    USING (profile_id = auth.uid())
    WITH CHECK (profile_id = auth.uid());

DROP POLICY IF EXISTS "Admin read all profile recurring cashflow" ON profile_recurring_cashflow;
CREATE POLICY "Admin read all profile recurring cashflow"
    ON profile_recurring_cashflow FOR SELECT
    USING (auth.role() = 'admin');

-- If this migration was applied earlier with description NOT NULL, relax it.
ALTER TABLE profile_recurring_cashflow
    ALTER COLUMN description DROP NOT NULL;
