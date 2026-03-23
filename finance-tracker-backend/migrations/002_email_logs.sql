-- Migration: 002_email_logs
-- Requires: 001_profiles_and_subscriptions.sql
-- Audit for product emails sent by this API (e.g. welcome via Resend template).

CREATE TABLE IF NOT EXISTS email_logs (
    id                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    profile_id           UUID REFERENCES profiles(id) ON DELETE SET NULL,
    recipient_email      TEXT NOT NULL,
    template_id          TEXT,
    status               TEXT NOT NULL
        CHECK (status IN ('sent', 'failed')),
    provider_message_id  TEXT,
    error_message        TEXT,
    created_at           TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

ALTER TABLE email_logs ENABLE ROW LEVEL SECURITY;

CREATE INDEX IF NOT EXISTS idx_email_logs_profile_id ON email_logs(profile_id);
CREATE INDEX IF NOT EXISTS idx_email_logs_created_at ON email_logs(created_at);

-- If upgrading from a version that had email_type: ALTER TABLE email_logs DROP COLUMN IF EXISTS email_type;
