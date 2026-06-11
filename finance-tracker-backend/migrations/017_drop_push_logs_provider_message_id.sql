-- Migration: 017_drop_push_logs_provider_message_id
-- Removes provider message id from push logs because Web Push does not provide a stable message id.

ALTER TABLE push_logs
    DROP COLUMN IF EXISTS provider_message_id;