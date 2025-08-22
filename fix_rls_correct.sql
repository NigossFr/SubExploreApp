-- ========================================
-- SCRIPT RLS CORRIGÉ POUR SUPABASE
-- ========================================

-- 1. D'abord, vérifier la structure de la table spot_types
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'spot_types' AND table_schema = 'public'
ORDER BY ordinal_position;

-- 2. ACTIVER RLS SUR LES TABLES PRINCIPALES
ALTER TABLE public.spots ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.spot_types ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;

-- 3. POLITIQUES TRÈS PERMISSIVES POUR LE DÉVELOPPEMENT
-- Supprimer les politiques existantes si elles existent
DROP POLICY IF EXISTS "dev_spot_types_all" ON public.spot_types;
DROP POLICY IF EXISTS "dev_spots_all" ON public.spots;
DROP POLICY IF EXISTS "dev_users_all" ON public.users;

-- Créer les nouvelles politiques
CREATE POLICY "dev_spot_types_all" ON public.spot_types FOR ALL USING (true);
CREATE POLICY "dev_spots_all" ON public.spots FOR ALL USING (true);
CREATE POLICY "dev_users_all" ON public.users FOR ALL USING (true);

-- 4. VÉRIFICATION
SELECT 
    tablename,
    rowsecurity,
    CASE 
        WHEN rowsecurity THEN '✅ RLS Activé' 
        ELSE '❌ RLS Désactivé' 
    END as status
FROM pg_tables 
WHERE schemaname = 'public' 
AND tablename IN ('spots', 'spot_types', 'users')
ORDER BY tablename;