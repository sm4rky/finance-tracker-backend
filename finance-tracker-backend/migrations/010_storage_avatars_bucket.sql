-- Migration: 010_storage_avatars_bucket
-- Supabase Storage: private bucket "avatars" + RLS on storage.objects
--
-- ---------------------------------------------------------------------------
-- Dashboard (Storage -> avatars -> Policies): if the SQL Editor returns 42501
-- "must be owner of table objects", create policies in the UI and paste the
-- expression below (target role: authenticated).
--
-- SELECT (USING) / INSERT (WITH CHECK) / UPDATE (USING + WITH CHECK) / DELETE (USING):
--   bucket_id = 'avatars' AND storage.extension(name) IN ('webp') AND (storage.foldername(name))[1] = auth.uid()::text
--
-- For UPDATE in the dashboard, use the same expression for both USING and WITH CHECK.
-- ---------------------------------------------------------------------------

INSERT INTO storage.buckets (id, name, public)
VALUES ('avatars', 'avatars', false)
ON CONFLICT (id) DO NOTHING;

UPDATE storage.buckets
SET public = false
WHERE id = 'avatars';

ALTER TABLE storage.objects ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "avatars_select_own" ON storage.objects;
DROP POLICY IF EXISTS "avatars_insert_own" ON storage.objects;
DROP POLICY IF EXISTS "avatars_update_own" ON storage.objects;
DROP POLICY IF EXISTS "avatars_delete_own" ON storage.objects;

CREATE POLICY "avatars_select_own"
    ON storage.objects FOR SELECT
    TO authenticated
    USING (
        bucket_id = 'avatars'
        AND storage.extension(name) IN ('webp')
        AND (storage.foldername(name))[1] = auth.uid()::text
    );

CREATE POLICY "avatars_insert_own"
    ON storage.objects FOR INSERT
    TO authenticated
    WITH CHECK (
        bucket_id = 'avatars'
        AND storage.extension(name) IN ('webp')
        AND (storage.foldername(name))[1] = auth.uid()::text
    );

CREATE POLICY "avatars_update_own"
    ON storage.objects FOR UPDATE
    TO authenticated
    USING (
        bucket_id = 'avatars'
        AND storage.extension(name) IN ('webp')
        AND (storage.foldername(name))[1] = auth.uid()::text
    )
    WITH CHECK (
        bucket_id = 'avatars'
        AND storage.extension(name) IN ('webp')
        AND (storage.foldername(name))[1] = auth.uid()::text
    );

CREATE POLICY "avatars_delete_own"
    ON storage.objects FOR DELETE
    TO authenticated
    USING (
        bucket_id = 'avatars'
        AND storage.extension(name) IN ('webp')
        AND (storage.foldername(name))[1] = auth.uid()::text
    );
