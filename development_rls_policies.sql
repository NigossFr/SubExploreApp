-- Script RLS simplifié pour le développement
-- À utiliser temporairement pendant les tests de l'application
-- ATTENTION : Plus permissif, à remplacer par les politiques strictes en production

-- ===========================================
-- ACTIVER RLS SUR LES TABLES PRINCIPALES
-- ===========================================

ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.spots ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.spot_types ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.spot_media ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.user_favorite_spots ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.user_preferences ENABLE ROW LEVEL SECURITY;

-- ===========================================
-- POLITIQUES DÉVELOPPEMENT (PLUS PERMISSIVES)
-- ===========================================

-- Supprime les politiques existantes si elles existent
DROP POLICY IF EXISTS "Development access for spot_types" ON public.spot_types;
DROP POLICY IF EXISTS "Development access for spots" ON public.spots;
DROP POLICY IF EXISTS "Development access for users" ON public.users;
DROP POLICY IF EXISTS "Development access for spot_media" ON public.spot_media;
DROP POLICY IF EXISTS "Development access for user_favorites" ON public.user_favorite_spots;
DROP POLICY IF EXISTS "Development access for user_preferences" ON public.user_preferences;

-- SPOT_TYPES : Accès libre en lecture, modification pour les authentifiés
CREATE POLICY "Development access for spot_types" ON public.spot_types
    FOR ALL USING (true);

-- SPOTS : Accès libre en lecture, modification pour les authentifiés
CREATE POLICY "Development access for spots" ON public.spots
    FOR ALL USING (true);

-- USERS : Les utilisateurs authentifiés peuvent voir tous les profils
CREATE POLICY "Development access for users" ON public.users
    FOR ALL USING (auth.role() = 'authenticated' OR auth.uid() = id);

-- SPOT_MEDIA : Accès libre
CREATE POLICY "Development access for spot_media" ON public.spot_media
    FOR ALL USING (true);

-- USER_FAVORITE_SPOTS : Accès pour les utilisateurs authentifiés
CREATE POLICY "Development access for user_favorites" ON public.user_favorite_spots
    FOR ALL USING (auth.role() = 'authenticated');

-- USER_PREFERENCES : Accès pour les utilisateurs authentifiés
CREATE POLICY "Development access for user_preferences" ON public.user_preferences
    FOR ALL USING (auth.role() = 'authenticated');

-- ===========================================
-- VÉRIFICATION
-- ===========================================

-- Vérifier que RLS est activé
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

-- Compter les politiques créées
SELECT 
    tablename,
    COUNT(*) as policy_count
FROM pg_policies 
WHERE schemaname = 'public'
GROUP BY tablename
ORDER BY tablename;