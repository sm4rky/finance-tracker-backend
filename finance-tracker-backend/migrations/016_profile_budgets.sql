-- Migration: 016_profile_budgets
-- Requires: 014_profile_custom_categories.sql

CREATE TABLE IF NOT EXISTS profile_budgets (
    id                              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    profile_id                      UUID NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
    name                            TEXT NOT NULL,
    amount_limit                    NUMERIC(18, 2) NOT NULL CHECK (amount_limit >= 0),
    is_recurring                    BOOLEAN NOT NULL,
    period_type                     TEXT
        CHECK (period_type IS NULL OR period_type IN ('WEEKLY', 'MONTHLY', 'YEARLY')),
    start_date                      DATE NOT NULL,
    end_date                        DATE,
    is_active                       BOOLEAN NOT NULL DEFAULT TRUE,
    profile_custom_category_set_id  UUID REFERENCES profile_custom_category_set(id) ON DELETE SET NULL,
    include_income                  BOOLEAN NOT NULL DEFAULT FALSE,
    include_unlinked_transactions   BOOLEAN NOT NULL DEFAULT TRUE,
    created_at                      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                      TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_profile_budgets_fixed_end_date
        CHECK (is_recurring OR end_date IS NOT NULL),
    CONSTRAINT chk_profile_budgets_recurring_period_type
        CHECK (NOT is_recurring OR period_type IS NOT NULL),
    CONSTRAINT chk_profile_budgets_date_order
        CHECK (end_date IS NULL OR end_date >= start_date)
);

CREATE TABLE IF NOT EXISTS profile_budget_categories (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    budget_id           UUID NOT NULL REFERENCES profile_budgets(id) ON DELETE CASCADE,
    pfc_primary_code    TEXT,
    pfc_version         TEXT,
    custom_category_id  UUID REFERENCES profile_custom_category(id) ON DELETE CASCADE,

    CONSTRAINT chk_profile_budget_categories_mapping_shape
        CHECK (
            (
                custom_category_id IS NOT NULL
                AND pfc_primary_code IS NULL
                AND pfc_version IS NULL
            )
            OR
            (
                custom_category_id IS NULL
                AND pfc_primary_code IS NOT NULL
                AND pfc_version IS NOT NULL
            )
        ),
    CONSTRAINT fk_profile_budget_categories_pfc
        FOREIGN KEY (pfc_version, pfc_primary_code)
        REFERENCES plaid_finance_category_primary(pfc_version, code)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,
    CONSTRAINT uq_profile_budget_categories_pfc
        UNIQUE (budget_id, pfc_primary_code, pfc_version),
    CONSTRAINT uq_profile_budget_categories_custom_category
        UNIQUE (budget_id, custom_category_id)
);

CREATE TABLE IF NOT EXISTS profile_budget_bank_accounts (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    budget_id               UUID NOT NULL REFERENCES profile_budgets(id) ON DELETE CASCADE,
    linked_bank_account_id  UUID NOT NULL REFERENCES linked_bank_accounts(id) ON DELETE CASCADE,
    CONSTRAINT uq_profile_budget_bank_accounts
        UNIQUE (budget_id, linked_bank_account_id)
);

CREATE TABLE IF NOT EXISTS profile_budget_periods (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    budget_id           UUID NOT NULL REFERENCES profile_budgets(id) ON DELETE CASCADE,
    period_start_date   DATE NOT NULL,
    period_end_date     DATE NOT NULL,
    period_name         TEXT NOT NULL,
    amount_limit        NUMERIC(18, 2) NOT NULL CHECK (amount_limit >= 0),
    spent_amount        NUMERIC(18, 2) NOT NULL DEFAULT 0 CHECK (spent_amount >= 0),
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_profile_budget_periods_date_order
        CHECK (period_end_date >= period_start_date),
    CONSTRAINT uq_profile_budget_periods_range
        UNIQUE (budget_id, period_start_date, period_end_date)
);

CREATE INDEX IF NOT EXISTS idx_profile_budgets_profile_id
    ON profile_budgets(profile_id);

CREATE INDEX IF NOT EXISTS idx_profile_budgets_profile_active
    ON profile_budgets(profile_id)
    WHERE is_active;

CREATE INDEX IF NOT EXISTS idx_profile_budget_categories_budget_id
    ON profile_budget_categories(budget_id);

CREATE INDEX IF NOT EXISTS idx_profile_budget_bank_accounts_budget_id
    ON profile_budget_bank_accounts(budget_id);

CREATE INDEX IF NOT EXISTS idx_profile_budget_periods_budget_id
    ON profile_budget_periods(budget_id);

CREATE INDEX IF NOT EXISTS idx_profile_budget_periods_budget_dates
    ON profile_budget_periods(budget_id, period_start_date, period_end_date);

ALTER TABLE profile_budgets ENABLE ROW LEVEL SECURITY;
ALTER TABLE profile_budget_categories ENABLE ROW LEVEL SECURITY;
ALTER TABLE profile_budget_bank_accounts ENABLE ROW LEVEL SECURITY;
ALTER TABLE profile_budget_periods ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Users manage own profile budgets" ON profile_budgets;
CREATE POLICY "Users manage own profile budgets"
    ON profile_budgets FOR ALL
    USING (profile_id = auth.uid())
    WITH CHECK (profile_id = auth.uid());

DROP POLICY IF EXISTS "Admin read all profile budgets" ON profile_budgets;
CREATE POLICY "Admin read all profile budgets"
    ON profile_budgets FOR SELECT
    USING (auth.role() = 'admin');

DROP POLICY IF EXISTS "Users manage own profile budget categories" ON profile_budget_categories;
CREATE POLICY "Users manage own profile budget categories"
    ON profile_budget_categories FOR ALL
    USING (
        EXISTS (
            SELECT 1
            FROM profile_budgets budget
            WHERE budget.id = profile_budget_categories.budget_id
              AND budget.profile_id = auth.uid()
        )
    )
    WITH CHECK (
        EXISTS (
            SELECT 1
            FROM profile_budgets budget
            WHERE budget.id = profile_budget_categories.budget_id
              AND budget.profile_id = auth.uid()
        )
    );

DROP POLICY IF EXISTS "Users manage own profile budget bank accounts" ON profile_budget_bank_accounts;
CREATE POLICY "Users manage own profile budget bank accounts"
    ON profile_budget_bank_accounts FOR ALL
    USING (
        EXISTS (
            SELECT 1
            FROM profile_budgets budget
            WHERE budget.id = profile_budget_bank_accounts.budget_id
              AND budget.profile_id = auth.uid()
        )
    )
    WITH CHECK (
        EXISTS (
            SELECT 1
            FROM profile_budgets budget
            WHERE budget.id = profile_budget_bank_accounts.budget_id
              AND budget.profile_id = auth.uid()
        )
    );

DROP POLICY IF EXISTS "Users manage own profile budget periods" ON profile_budget_periods;
CREATE POLICY "Users manage own profile budget periods"
    ON profile_budget_periods FOR ALL
    USING (
        EXISTS (
            SELECT 1
            FROM profile_budgets budget
            WHERE budget.id = profile_budget_periods.budget_id
              AND budget.profile_id = auth.uid()
        )
    )
    WITH CHECK (
        EXISTS (
            SELECT 1
            FROM profile_budgets budget
            WHERE budget.id = profile_budget_periods.budget_id
              AND budget.profile_id = auth.uid()
        )
    );
