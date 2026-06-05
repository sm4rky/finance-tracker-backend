-- Migration: 015_uppercase_plaid_finance_category_primary_pfc_version
-- Normalize Plaid finance category primary versions to match Plaid enum casing.

UPDATE plaid_finance_category_primary
SET pfc_version = CASE pfc_version
    WHEN 'v1' THEN 'V1'
    WHEN 'v2' THEN 'V2'
    ELSE pfc_version
END
WHERE pfc_version IN ('v1', 'v2');
