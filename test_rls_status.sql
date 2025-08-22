-- Script pour vérifier l'état des politiques RLS sur Supabase
-- À exécuter dans l'éditeur SQL de Supabase

-- 1. Vérifier que RLS est activé sur les tables principales
SELECT 
    schemaname, 
    tablename, 
    rowsecurity,
    CASE 
        WHEN rowsecurity THEN '✅ RLS Activé' 
        ELSE '❌ RLS Désactivé' 
    END as status
FROM pg_tables 
WHERE schemaname = 'public' 
AND tablename IN ('users', 'spots', 'spot_types', 'spot_media', 'user_favorite_spots', 'user_preferences')
ORDER BY tablename;

-- 2. Compter les politiques par table
SELECT 
    tablename,
    COUNT(*) as policy_count,
    string_agg(policyname, ', ') as policies
FROM pg_policies 
WHERE schemaname = 'public'
AND tablename IN ('users', 'spots', 'spot_types', 'spot_media', 'user_favorite_spots', 'user_preferences')
GROUP BY tablename
ORDER BY tablename;

-- 3. Vérifier le contenu des tables principales
SELECT 'spot_types' as table_name, COUNT(*) as row_count FROM spot_types
UNION ALL
SELECT 'spots' as table_name, COUNT(*) as row_count FROM spots
UNION ALL
SELECT 'users' as table_name, COUNT(*) as row_count FROM users
ORDER BY table_name;

-- 4. Si aucune politique n'existe, voici un test simple
-- (À décommenter si besoin de créer une politique de test)
/*
-- Politique de test pour spot_types (lecture publique)
CREATE POLICY "test_public_read_spot_types" ON public.spot_types
    FOR SELECT USING (true);
*/