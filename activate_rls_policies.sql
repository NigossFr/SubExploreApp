-- Script pour activer Row Level Security (RLS) et créer les politiques
-- À exécuter dans l'éditeur SQL de Supabase

-- ===========================================
-- 1. ACTIVER RLS SUR TOUTES LES TABLES
-- ===========================================

-- Tables principales
ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.user_preferences ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.spots ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.spot_types ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.spot_media ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.user_favorite_spots ENABLE ROW LEVEL SECURITY;

-- Tables de tokens/sécurité
ALTER TABLE public.email_verification_tokens ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.password_reset_tokens ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.revoked_tokens ENABLE ROW LEVEL SECURITY;

-- Table de migrations (peut rester sans RLS pour les opérations système)
-- ALTER TABLE public.__EFMigrationsHistory ENABLE ROW LEVEL SECURITY;

-- Table spatial_ref_sys (système PostGIS, généralement pas besoin de RLS)
-- ALTER TABLE public.spatial_ref_sys ENABLE ROW LEVEL SECURITY;

-- ===========================================
-- 2. POLITIQUES POUR LES SPOT_TYPES (lecture publique)
-- ===========================================

-- Tout le monde peut lire les types de spots
CREATE POLICY "Public read access for spot_types" ON public.spot_types
    FOR SELECT USING (true);

-- Seuls les administrateurs peuvent modifier les types de spots
CREATE POLICY "Admin only access for spot_types modifications" ON public.spot_types
    FOR ALL USING (
        auth.uid() IN (
            SELECT id FROM public.users 
            WHERE account_type = 'Admin' OR moderator_status = 'Active'
        )
    );

-- ===========================================
-- 3. POLITIQUES POUR LES SPOTS
-- ===========================================

-- Lecture publique des spots validés
CREATE POLICY "Public read access for validated spots" ON public.spots
    FOR SELECT USING (
        status = 'validated' OR 
        status = 'published' OR
        created_by = auth.uid()
    );

-- Les utilisateurs connectés peuvent créer des spots
CREATE POLICY "Authenticated users can create spots" ON public.spots
    FOR INSERT WITH CHECK (auth.role() = 'authenticated');

-- Les utilisateurs peuvent modifier leurs propres spots
CREATE POLICY "Users can edit their own spots" ON public.spots
    FOR UPDATE USING (created_by = auth.uid());

-- Les modérateurs peuvent tout modifier
CREATE POLICY "Moderators can edit all spots" ON public.spots
    FOR ALL USING (
        auth.uid() IN (
            SELECT id FROM public.users 
            WHERE account_type = 'Admin' OR moderator_status = 'Active'
        )
    );

-- ===========================================
-- 4. POLITIQUES POUR LES UTILISATEURS
-- ===========================================

-- Les utilisateurs peuvent voir leur propre profil
CREATE POLICY "Users can view their own profile" ON public.users
    FOR SELECT USING (id = auth.uid());

-- Les utilisateurs peuvent modifier leur propre profil
CREATE POLICY "Users can update their own profile" ON public.users
    FOR UPDATE USING (id = auth.uid());

-- Les modérateurs peuvent voir tous les profils
CREATE POLICY "Moderators can view all profiles" ON public.users
    FOR SELECT USING (
        auth.uid() IN (
            SELECT id FROM public.users 
            WHERE account_type = 'Admin' OR moderator_status = 'Active'
        )
    );

-- ===========================================
-- 5. POLITIQUES POUR LES PRÉFÉRENCES UTILISATEUR
-- ===========================================

-- Les utilisateurs peuvent gérer leurs propres préférences
CREATE POLICY "Users can manage their own preferences" ON public.user_preferences
    FOR ALL USING (user_id = auth.uid());

-- ===========================================
-- 6. POLITIQUES POUR LES FAVORIS
-- ===========================================

-- Les utilisateurs peuvent gérer leurs propres favoris
CREATE POLICY "Users can manage their own favorites" ON public.user_favorite_spots
    FOR ALL USING (user_id = auth.uid());

-- ===========================================
-- 7. POLITIQUES POUR LES MÉDIAS
-- ===========================================

-- Lecture publique des médias de spots validés
CREATE POLICY "Public read access for validated spot media" ON public.spot_media
    FOR SELECT USING (
        spot_id IN (
            SELECT id FROM public.spots 
            WHERE status = 'validated' OR status = 'published'
        ) OR
        spot_id IN (
            SELECT id FROM public.spots 
            WHERE created_by = auth.uid()
        )
    );

-- Les utilisateurs peuvent ajouter des médias à leurs spots
CREATE POLICY "Users can add media to their spots" ON public.spot_media
    FOR INSERT WITH CHECK (
        spot_id IN (
            SELECT id FROM public.spots 
            WHERE created_by = auth.uid()
        )
    );

-- Les utilisateurs peuvent modifier les médias de leurs spots
CREATE POLICY "Users can edit media of their spots" ON public.spot_media
    FOR UPDATE USING (
        spot_id IN (
            SELECT id FROM public.spots 
            WHERE created_by = auth.uid()
        )
    );

-- ===========================================
-- 8. POLITIQUES POUR LES TOKENS (sécurité stricte)
-- ===========================================

-- Email verification tokens - seul le propriétaire peut voir
CREATE POLICY "Users can view their own email tokens" ON public.email_verification_tokens
    FOR SELECT USING (user_id = auth.uid());

CREATE POLICY "Users can delete their own email tokens" ON public.email_verification_tokens
    FOR DELETE USING (user_id = auth.uid());

-- Password reset tokens - seul le propriétaire peut voir
CREATE POLICY "Users can view their own password reset tokens" ON public.password_reset_tokens
    FOR SELECT USING (user_id = auth.uid());

CREATE POLICY "Users can delete their own password reset tokens" ON public.password_reset_tokens
    FOR DELETE USING (user_id = auth.uid());

-- Revoked tokens - seul le propriétaire peut voir
CREATE POLICY "Users can view their own revoked tokens" ON public.revoked_tokens
    FOR SELECT USING (user_id = auth.uid());

-- ===========================================
-- 9. FONCTION HELPER POUR VÉRIFIER LES RÔLES
-- ===========================================

-- Fonction pour vérifier si l'utilisateur est admin/modérateur
CREATE OR REPLACE FUNCTION public.is_admin_or_moderator()
RETURNS boolean AS $$
BEGIN
    RETURN EXISTS (
        SELECT 1 FROM public.users 
        WHERE id = auth.uid() 
        AND (account_type = 'Admin' OR moderator_status = 'Active')
    );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ===========================================
-- 10. VÉRIFICATION DES POLITIQUES CRÉÉES
-- ===========================================

-- Vérifier que RLS est activé
SELECT schemaname, tablename, rowsecurity 
FROM pg_tables 
WHERE schemaname = 'public' 
AND tablename IN ('users', 'spots', 'spot_types', 'spot_media', 'user_favorite_spots', 'user_preferences');

-- Vérifier les politiques créées
SELECT schemaname, tablename, policyname, permissive, roles, cmd, qual 
FROM pg_policies 
WHERE schemaname = 'public'
ORDER BY tablename, policyname;