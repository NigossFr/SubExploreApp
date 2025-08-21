-- ========================================
-- NETTOYAGE COMPLET BASE SUPABASE
-- ========================================
-- ⚠️ ATTENTION: Ce script supprime TOUT !
-- Utilisez uniquement si vous voulez repartir de zéro

-- ========================================
-- ÉTAPE 1: Supprimer toutes les tables existantes
-- ========================================

-- Supprimer les tables dans l'ordre des dépendances (enfant → parent)
DROP TABLE IF EXISTS spot_media CASCADE;
DROP TABLE IF EXISTS user_favorite_spots CASCADE;
DROP TABLE IF EXISTS spots CASCADE;
DROP TABLE IF EXISTS user_preferences CASCADE;
DROP TABLE IF EXISTS email_verification_tokens CASCADE;
DROP TABLE IF EXISTS password_reset_tokens CASCADE;
DROP TABLE IF EXISTS revoked_tokens CASCADE;
DROP TABLE IF EXISTS spot_types CASCADE;
DROP TABLE IF EXISTS users CASCADE;

-- Supprimer d'autres tables qui pourraient exister
DROP TABLE IF EXISTS temp_spot_types_backup CASCADE;

-- ========================================
-- ÉTAPE 2: Supprimer tous les types ENUM
-- ========================================

DROP TYPE IF EXISTS activity_category CASCADE;
DROP TYPE IF EXISTS account_type CASCADE;
DROP TYPE IF EXISTS subscription_status CASCADE;
DROP TYPE IF EXISTS expertise_level CASCADE;

-- Supprimer les anciens enums qui pourraient traîner
DROP TYPE IF EXISTS moderator_specialization CASCADE;
DROP TYPE IF EXISTS moderator_status CASCADE;
DROP TYPE IF EXISTS user_permissions CASCADE;

-- ========================================
-- ÉTAPE 3: Supprimer tous les index qui pourraient rester
-- ========================================

DROP INDEX IF EXISTS idx_users_email;
DROP INDEX IF EXISTS idx_users_username;
DROP INDEX IF EXISTS idx_spots_location;
DROP INDEX IF EXISTS idx_spots_type;
DROP INDEX IF EXISTS idx_spots_creator;
DROP INDEX IF EXISTS idx_spots_validation_status;
DROP INDEX IF EXISTS idx_user_favorites_user;
DROP INDEX IF EXISTS idx_user_favorites_spot;
DROP INDEX IF EXISTS idx_spot_media_spot;
DROP INDEX IF EXISTS idx_email_tokens_hash;
DROP INDEX IF EXISTS idx_password_tokens_hash;
DROP INDEX IF EXISTS idx_revoked_tokens_hash;

-- ========================================
-- ÉTAPE 4: Nettoyer les séquences et fonctions
-- ========================================

-- Supprimer les séquences qui pourraient rester
DROP SEQUENCE IF EXISTS users_id_seq CASCADE;
DROP SEQUENCE IF EXISTS spots_id_seq CASCADE;
DROP SEQUENCE IF EXISTS spot_types_id_seq CASCADE;

-- ========================================
-- ÉTAPE 5: Vérification du nettoyage
-- ========================================

-- Vérifier qu'il ne reste plus de tables
SELECT 'Tables restantes:' as info;
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
  AND table_type = 'BASE TABLE'
ORDER BY table_name;

-- Vérifier qu'il ne reste plus d'enums
SELECT 'Enums restants:' as info;
SELECT typname 
FROM pg_type 
WHERE typtype = 'e' 
  AND typnamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'public')
ORDER BY typname;

SELECT '🧹 NETTOYAGE TERMINÉ !' as status;
SELECT 'Votre base de données est maintenant complètement vide et prête' as message;
SELECT 'Vous pouvez maintenant exécuter le script create_supabase_schema.sql' as next_step;