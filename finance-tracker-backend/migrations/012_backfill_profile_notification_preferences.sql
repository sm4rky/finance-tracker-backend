-- Migration: 012_backfill_profile_notification_preferences
-- Ensures every existing profile has default notification preferences.

INSERT INTO profile_notification_preferences (
    id,
    profile_id,
    email_enabled,
    due_reminder_enabled,
    reminder_days_before,
    budget_alert_enabled,
    budget_alert_threshold,
    monthly_statement_enabled,
    created_at,
    updated_at
)
SELECT
    gen_random_uuid(),
    p.id,
    FALSE,
    FALSE,
    1,
    FALSE,
    80,
    FALSE,
    NOW(),
    NOW()
FROM profiles p
WHERE NOT EXISTS (
    SELECT 1
    FROM profile_notification_preferences pref
    WHERE pref.profile_id = p.id
);
