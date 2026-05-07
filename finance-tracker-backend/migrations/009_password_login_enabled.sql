-- Migration: 009_password_login_enabled
-- App-managed flag: user has enabled email+password login (set true after successful
-- Supabase Auth password update from the app; default false for OAuth-only users).

ALTER TABLE profiles
    ADD COLUMN IF NOT EXISTS password_login_enabled BOOLEAN NOT NULL DEFAULT FALSE;