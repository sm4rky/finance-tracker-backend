-- Migration: 011_notification_preferences_and_push
-- Adds profile-level notification preferences, per-device/browser push subscriptions,
-- and push delivery logs.

CREATE TABLE IF NOT EXISTS profile_notification_preferences (
    id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    profile_id                  UUID NOT NULL UNIQUE REFERENCES profiles(id) ON DELETE CASCADE,
    email_enabled               BOOLEAN NOT NULL DEFAULT FALSE,
    due_reminder_enabled        BOOLEAN NOT NULL DEFAULT FALSE,
    reminder_days_before        INTEGER NOT NULL DEFAULT 1,
    budget_alert_enabled        BOOLEAN NOT NULL DEFAULT FALSE,
    budget_alert_threshold      INTEGER NOT NULL DEFAULT 80,
    monthly_statement_enabled   BOOLEAN NOT NULL DEFAULT FALSE,
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_profile_notification_preferences_reminder_days_before
        CHECK (reminder_days_before > 0),
    CONSTRAINT chk_profile_notification_preferences_budget_alert_threshold
        CHECK (budget_alert_threshold > 0 AND budget_alert_threshold <= 100)
);

CREATE TABLE IF NOT EXISTS push_subscriptions (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    profile_id          UUID NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
    endpoint            TEXT NOT NULL UNIQUE,
    p256dh              TEXT NOT NULL,
    auth                TEXT NOT NULL,
    user_agent          TEXT,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_push_subscriptions_profile_id
    ON push_subscriptions(profile_id);

CREATE UNIQUE INDEX IF NOT EXISTS idx_push_subscriptions_endpoint
    ON push_subscriptions(endpoint);

CREATE TABLE IF NOT EXISTS push_logs (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    profile_id              UUID REFERENCES profiles(id) ON DELETE SET NULL,
    push_subscription_id    UUID REFERENCES push_subscriptions(id) ON DELETE SET NULL,
    dedupe_key              TEXT,
    status                  TEXT NOT NULL,
    provider_message_id     TEXT,
    error_message           TEXT,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_push_logs_status
        CHECK (status IN ('sent', 'failed'))
);

CREATE INDEX IF NOT EXISTS idx_push_logs_profile_id_created_at
    ON push_logs(profile_id, created_at);

CREATE INDEX IF NOT EXISTS idx_push_logs_push_subscription_id
    ON push_logs(push_subscription_id);

CREATE UNIQUE INDEX IF NOT EXISTS idx_push_logs_profile_id_dedupe_key
    ON push_logs(profile_id, dedupe_key)
    WHERE dedupe_key IS NOT NULL;
