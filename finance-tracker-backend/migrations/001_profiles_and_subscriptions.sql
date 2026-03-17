-- Migration: 001_profiles_and_subscriptions
-- Tables: profiles, subscription_plans, profile_subscriptions, subscription_history (audit), subscription_payments (revenue)

-- ---------------------------------------------------------------------------
-- profiles: one row per auth user, extended profile data
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS profiles (
    id                  UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    email               TEXT UNIQUE,
    username            TEXT UNIQUE,
    full_name           TEXT,
    avatar_url          TEXT,
    role                TEXT NOT NULL DEFAULT 'user'
        CHECK (role IN ('user', 'admin')),
    created_at          TIMESTAMPTZ DEFAULT NOW(),
    updated_at          TIMESTAMPTZ DEFAULT NOW()
);

ALTER TABLE profiles ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Users see own profile" ON profiles;
CREATE POLICY "Users see own profile"
    ON profiles FOR ALL
    USING (id = auth.uid())
    WITH CHECK (id = auth.uid());

DROP POLICY IF EXISTS "Admin read all profiles" ON profiles;
CREATE POLICY "Admin read all profiles"
    ON profiles FOR SELECT
    USING (auth.role() = 'admin');

CREATE INDEX IF NOT EXISTS idx_profiles_email ON profiles(email);
CREATE INDEX IF NOT EXISTS idx_profiles_username ON profiles(username);

-- ---------------------------------------------------------------------------
-- subscription_plans: static plans (read-only for users, admin manages)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS subscription_plans (
    id                  TEXT PRIMARY KEY,
    name                TEXT NOT NULL,
    max_accounts        INTEGER,
    history_months      INTEGER,
    has_ai              BOOLEAN NOT NULL DEFAULT FALSE,
    monthly_price       NUMERIC
);

INSERT INTO subscription_plans (id, name, max_accounts, history_months, has_ai, monthly_price)
VALUES
    ('free',     'Free',     2,    24,   FALSE,  0.00),
    ('pro',      'Pro',      5,    60,   TRUE,   9.99),
    ('premium',  'Premium',  NULL, NULL,  TRUE,   19.99)
ON CONFLICT (id) DO NOTHING;

ALTER TABLE subscription_plans ENABLE ROW LEVEL SECURITY;

-- Public read: anyone (including anon) can view plans (e.g. pricing page before login).
DROP POLICY IF EXISTS "Public read subscription plans" ON subscription_plans;
CREATE POLICY "Public read subscription plans"
    ON subscription_plans FOR SELECT
    USING (true);

-- ---------------------------------------------------------------------------
-- profile_subscriptions: links profile to plan (one row per profile)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS profile_subscriptions (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    profile_id          UUID NOT NULL REFERENCES profiles(id) ON DELETE CASCADE UNIQUE,
    plan_id             TEXT NOT NULL REFERENCES subscription_plans(id),

    start_date          TIMESTAMPTZ,
    end_date            TIMESTAMPTZ,
    canceled_at         TIMESTAMPTZ,

    created_at          TIMESTAMPTZ DEFAULT NOW(),
    updated_at          TIMESTAMPTZ DEFAULT NOW()
);

ALTER TABLE profile_subscriptions ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Users see own subscription" ON profile_subscriptions;
CREATE POLICY "Users see own subscription"
    ON profile_subscriptions FOR ALL
    USING (profile_id = auth.uid())
    WITH CHECK (profile_id = auth.uid());

DROP POLICY IF EXISTS "Admin read all subscriptions" ON profile_subscriptions;
CREATE POLICY "Admin read all subscriptions"
    ON profile_subscriptions FOR SELECT
    USING (auth.role() = 'admin');

CREATE INDEX IF NOT EXISTS idx_profile_subscriptions_profile_id ON profile_subscriptions(profile_id);
CREATE INDEX IF NOT EXISTS idx_profile_subscriptions_end_date ON profile_subscriptions(end_date);

-- ---------------------------------------------------------------------------
-- subscription_history: audit log for subscription changes (support, analytics, compliance)
-- ON DELETE SET NULL on profile_id: when profile is deleted, keep history rows for audit; profile_id becomes NULL.
-- event_type: upgraded, canceled (-> free), downgraded, refunded, renewed (from_plan_id = to_plan_id)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS subscription_history (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    profile_id          UUID REFERENCES profiles(id) ON DELETE SET NULL,

    event_type          TEXT NOT NULL
        CHECK (event_type IN ('upgraded', 'canceled', 'downgraded', 'refunded', 'renewed')),
    from_plan_id        TEXT REFERENCES subscription_plans(id),
    to_plan_id          TEXT REFERENCES subscription_plans(id),

    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    effective_at        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

ALTER TABLE subscription_history ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Users see own subscription history" ON subscription_history;
CREATE POLICY "Users see own subscription history"
    ON subscription_history FOR SELECT
    USING (profile_id = auth.uid());

DROP POLICY IF EXISTS "Admin read all subscription history" ON subscription_history;
CREATE POLICY "Admin read all subscription history"
    ON subscription_history FOR SELECT
    USING (auth.role() = 'admin');

CREATE INDEX IF NOT EXISTS idx_subscription_history_profile_id ON subscription_history(profile_id);
CREATE INDEX IF NOT EXISTS idx_subscription_history_created_at ON subscription_history(created_at);
CREATE INDEX IF NOT EXISTS idx_subscription_history_effective_at ON subscription_history(effective_at);
CREATE INDEX IF NOT EXISTS idx_subscription_history_event_type ON subscription_history(event_type);

-- ---------------------------------------------------------------------------
-- subscription_payments: every charge/refund/credit (source of truth for revenue)
-- ON DELETE SET NULL on profile_id: when profile is deleted, keep payment rows for revenue/audit; profile_id becomes NULL.
-- payment_type: charge (money in), refund (money back), credit; revenue = SUM(amount) WHERE payment_type = 'charge'.
-- plan_id = plan this payment is for (charge) or credited from (refund/credit).
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS subscription_payments (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    profile_id          UUID REFERENCES profiles(id) ON DELETE SET NULL,

    amount              NUMERIC NOT NULL,
    charged_at          TIMESTAMPTZ NOT NULL,
    plan_id             TEXT NOT NULL REFERENCES subscription_plans(id),

    payment_type        TEXT NOT NULL
        CHECK (payment_type IN ('charge', 'refund', 'credit')),
    reference           TEXT,

    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

ALTER TABLE subscription_payments ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Users see own payments" ON subscription_payments;
CREATE POLICY "Users see own payments"
    ON subscription_payments FOR SELECT
    USING (profile_id = auth.uid());

DROP POLICY IF EXISTS "Admin read all payments" ON subscription_payments;
CREATE POLICY "Admin read all payments"
    ON subscription_payments FOR SELECT
    USING (auth.role() = 'admin');

CREATE INDEX IF NOT EXISTS idx_subscription_payments_profile_id ON subscription_payments(profile_id);
CREATE INDEX IF NOT EXISTS idx_subscription_payments_charged_at ON subscription_payments(charged_at);
CREATE INDEX IF NOT EXISTS idx_subscription_payments_payment_type ON subscription_payments(payment_type);
