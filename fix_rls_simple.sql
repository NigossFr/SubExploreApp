-- ========================================
-- SCRIPT RLS SIMPLE POUR SUPABASE
-- Copier-coller directement dans l'éditeur SQL
-- ========================================

-- 1. ACTIVER RLS SUR LES TABLES PRINCIPALES
ALTER TABLE public.spots ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.spot_types ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;

-- 2. POLITIQUES TRÈS PERMISSIVES POUR LE DÉVELOPPEMENT
-- (À remplacer par des politiques strictes en production)

-- SPOT_TYPES : Accès libre pour tous
CREATE POLICY "dev_spot_types_all" ON public.spot_types FOR ALL USING (true);

-- SPOTS : Accès libre pour tous
CREATE POLICY "dev_spots_all" ON public.spots FOR ALL USING (true);

-- USERS : Accès libre pour tous (développement uniquement!)
CREATE POLICY "dev_users_all" ON public.users FOR ALL USING (true);

-- 3. VÉRIFICATION
SELECT 'RLS activé sur spot_types' as message WHERE (SELECT rowsecurity FROM pg_tables WHERE tablename = 'spot_types' AND schemaname = 'public');
SELECT 'RLS activé sur spots' as message WHERE (SELECT rowsecurity FROM pg_tables WHERE tablename = 'spots' AND schemaname = 'public');
SELECT 'RLS activé sur users' as message WHERE (SELECT rowsecurity FROM pg_tables WHERE tablename = 'users' AND schemaname = 'public');