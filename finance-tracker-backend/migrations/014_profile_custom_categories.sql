-- Migration: 014_profile_custom_categories
-- User-defined category sets, categories, and mappings to Plaid PFC primary codes.

CREATE TABLE IF NOT EXISTS profile_custom_category_set (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    profile_id  UUID NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
    name        TEXT NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS profile_custom_category (
    id                              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    profile_custom_category_set_id  UUID NOT NULL REFERENCES profile_custom_category_set(id) ON DELETE CASCADE,
    name                            TEXT NOT NULL,
    color_set                       TEXT NOT NULL,
    icon_name                       TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS profile_custom_category_pfc_primary (
    id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    profile_custom_category_id  UUID NOT NULL REFERENCES profile_custom_category(id) ON DELETE CASCADE,
    pfc_primary_code            TEXT NOT NULL,
    pfc_version                 TEXT NOT NULL,

    CONSTRAINT fk_profile_custom_category_pfc_primary_pfc
        FOREIGN KEY (pfc_version, pfc_primary_code)
        REFERENCES plaid_finance_category_primary(pfc_version, code)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,
    CONSTRAINT uq_profile_custom_category_pfc_primary_mapping
        UNIQUE (profile_custom_category_id, pfc_primary_code, pfc_version)
);

CREATE INDEX IF NOT EXISTS idx_profile_custom_category_set_profile_id
    ON profile_custom_category_set(profile_id);

CREATE INDEX IF NOT EXISTS idx_profile_custom_category_set_profile_name
    ON profile_custom_category_set(profile_id, name);

CREATE INDEX IF NOT EXISTS idx_profile_custom_category_set_id
    ON profile_custom_category(profile_custom_category_set_id);

CREATE INDEX IF NOT EXISTS idx_profile_custom_category_pfc_primary_category_id
    ON profile_custom_category_pfc_primary(profile_custom_category_id);

CREATE INDEX IF NOT EXISTS idx_profile_custom_category_pfc_primary_pfc
    ON profile_custom_category_pfc_primary(pfc_version, pfc_primary_code);

ALTER TABLE profile_custom_category_set ENABLE ROW LEVEL SECURITY;
ALTER TABLE profile_custom_category ENABLE ROW LEVEL SECURITY;
ALTER TABLE profile_custom_category_pfc_primary ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Users manage own custom category sets" ON profile_custom_category_set;
CREATE POLICY "Users manage own custom category sets"
    ON profile_custom_category_set FOR ALL
    USING (profile_id = auth.uid())
    WITH CHECK (profile_id = auth.uid());

DROP POLICY IF EXISTS "Admin read all custom category sets" ON profile_custom_category_set;
CREATE POLICY "Admin read all custom category sets"
    ON profile_custom_category_set FOR SELECT
    USING (auth.role() = 'admin');

DROP POLICY IF EXISTS "Users manage own custom categories" ON profile_custom_category;
CREATE POLICY "Users manage own custom categories"
    ON profile_custom_category FOR ALL
    USING (
        EXISTS (
            SELECT 1
            FROM profile_custom_category_set category_set
            WHERE category_set.id = profile_custom_category.profile_custom_category_set_id
              AND category_set.profile_id = auth.uid()
        )
    )
    WITH CHECK (
        EXISTS (
            SELECT 1
            FROM profile_custom_category_set category_set
            WHERE category_set.id = profile_custom_category.profile_custom_category_set_id
              AND category_set.profile_id = auth.uid()
        )
    );

DROP POLICY IF EXISTS "Admin read all custom categories" ON profile_custom_category;
CREATE POLICY "Admin read all custom categories"
    ON profile_custom_category FOR SELECT
    USING (auth.role() = 'admin');

DROP POLICY IF EXISTS "Users manage own custom category pfc mappings" ON profile_custom_category_pfc_primary;
CREATE POLICY "Users manage own custom category pfc mappings"
    ON profile_custom_category_pfc_primary FOR ALL
    USING (
        EXISTS (
            SELECT 1
            FROM profile_custom_category category
            JOIN profile_custom_category_set category_set
              ON category_set.id = category.profile_custom_category_set_id
            WHERE category.id = profile_custom_category_pfc_primary.profile_custom_category_id
              AND category_set.profile_id = auth.uid()
        )
    )
    WITH CHECK (
        EXISTS (
            SELECT 1
            FROM profile_custom_category category
            JOIN profile_custom_category_set category_set
              ON category_set.id = category.profile_custom_category_set_id
            WHERE category.id = profile_custom_category_pfc_primary.profile_custom_category_id
              AND category_set.profile_id = auth.uid()
        )
    );

DROP POLICY IF EXISTS "Admin read all custom category pfc mappings" ON profile_custom_category_pfc_primary;
CREATE POLICY "Admin read all custom category pfc mappings"
    ON profile_custom_category_pfc_primary FOR SELECT
    USING (auth.role() = 'admin');
