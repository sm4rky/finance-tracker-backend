-- Migration: 005_plaid_update_mode
-- Requires: 004_transactions_and_cursor.sql
-- Plaid Link session intent; pending deselected account ids after update-mode exchange (user confirms before soft-delete/tx status).

ALTER TABLE plaid_link_sessions
    ADD COLUMN IF NOT EXISTS intent TEXT NOT NULL DEFAULT 'connect'
        CHECK (intent IN ('connect', 'relink', 'update'));

ALTER TABLE linked_banks
    ADD COLUMN IF NOT EXISTS pending_deselected_plaid_account_ids JSONB;

COMMENT ON COLUMN linked_banks.pending_deselected_plaid_account_ids IS
    'After update-mode Link exchange: Plaid account_ids no longer in Item; user must confirm retain vs purge before accounts are deactivated.';
