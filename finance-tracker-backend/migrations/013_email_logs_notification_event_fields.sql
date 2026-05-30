-- Migration: 013_email_logs_notification_dedupe_key
-- Aligns email logs with notification dedupe conventions used by push_logs.

ALTER TABLE email_logs
    ADD COLUMN IF NOT EXISTS dedupe_key TEXT;

CREATE INDEX IF NOT EXISTS idx_email_logs_profile_id_created_at
    ON email_logs(profile_id, created_at);

CREATE UNIQUE INDEX IF NOT EXISTS idx_email_logs_profile_id_dedupe_key
    ON email_logs(profile_id, dedupe_key)
    WHERE dedupe_key IS NOT NULL;
