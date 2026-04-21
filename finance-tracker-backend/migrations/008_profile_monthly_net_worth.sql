-- Monthly net worth (one row per profile per calendar month, UTC).
-- Written by Hangfire at 00:00 UTC on the 1st of each month (see Hangfire:MonthlyNetWorthCron).
-- period_start_date is always the first calendar day of that month (e.g. 2026-03-01 = March 2026).

CREATE TABLE IF NOT EXISTS profile_monthly_net_worth (
    id                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    profile_id           UUID NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
    period_start_date    DATE NOT NULL,
    total_assets         NUMERIC(18, 2) NOT NULL,
    total_liabilities    NUMERIC(18, 2) NOT NULL,
    net_worth            NUMERIC(18, 2) NOT NULL,
    created_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_profile_monthly_net_worth_period UNIQUE (profile_id, period_start_date)
);

CREATE INDEX IF NOT EXISTS idx_profile_monthly_net_worth_profile_period
    ON profile_monthly_net_worth(profile_id, period_start_date DESC);

ALTER TABLE profile_monthly_net_worth ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Users read own monthly net worth" ON profile_monthly_net_worth;
CREATE POLICY "Users read own monthly net worth"
    ON profile_monthly_net_worth FOR SELECT
    USING (profile_id = auth.uid());
