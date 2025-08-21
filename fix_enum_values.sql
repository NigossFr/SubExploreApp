-- ========================================
-- CORRECTION DES VALEURS ENUM POUR SUPABASE
-- ========================================
-- Aligner les enums PostgreSQL avec le code C# MAUI

-- ========================================
-- 🔧 ÉTAPE 1: Supprimer et recréer les enums incorrects
-- ========================================

-- Supprimer les contraintes et colonnes qui utilisent les enums
ALTER TABLE users DROP CONSTRAINT IF EXISTS users_account_type_check;
ALTER TABLE users DROP CONSTRAINT IF EXISTS users_subscription_status_check;
ALTER TABLE users DROP CONSTRAINT IF EXISTS users_expertise_level_check;

-- Supprimer et recréer account_type (correct)
DROP TYPE IF EXISTS account_type CASCADE;
CREATE TYPE account_type AS ENUM (
    'Standard',           -- 0: Utilisateur standard
    'ExpertModerator',    -- 1: Modérateur expert  
    'VerifiedProfessional', -- 2: Professionnel vérifié
    'Administrator'       -- 3: Administrateur
);

-- Supprimer et recréer subscription_status (CORRECTION NÉCESSAIRE)
DROP TYPE IF EXISTS subscription_status CASCADE;
CREATE TYPE subscription_status AS ENUM (
    'Free',          -- Gratuit
    'Premium',       -- Premium
    'PremiumPlus',   -- Premium Plus (au lieu de Enterprise)
    'Suspended'      -- Suspendu
);

-- Supprimer et recréer expertise_level (CORRECTION NÉCESSAIRE)
DROP TYPE IF EXISTS expertise_level CASCADE;
CREATE TYPE expertise_level AS ENUM (
    'Beginner',      -- 0: Débutant
    'Intermediate',  -- 1: Intermédiaire
    'Advanced',      -- 2: Avancé
    'Expert',        -- 3: Expert (au lieu de Professional)
    'Professional'   -- 4: Professionnel
);

-- ========================================
-- 🔧 ÉTAPE 2: Recréer les colonnes avec les nouveaux types
-- ========================================

-- Ajouter temporairement les nouvelles colonnes
ALTER TABLE users ADD COLUMN account_type_new account_type NOT NULL DEFAULT 'Standard';
ALTER TABLE users ADD COLUMN subscription_status_new subscription_status NOT NULL DEFAULT 'Free';
ALTER TABLE users ADD COLUMN expertise_level_new expertise_level;

-- Migrer les données existantes avec mapping correct
UPDATE users SET 
    account_type_new = CASE 
        WHEN account_type = 'Administrator' THEN 'Administrator'::account_type
        ELSE 'Standard'::account_type 
    END,
    subscription_status_new = CASE 
        WHEN subscription_status = 'Enterprise' THEN 'PremiumPlus'::subscription_status
        WHEN subscription_status = 'Premium' THEN 'Premium'::subscription_status
        ELSE 'Free'::subscription_status 
    END,
    expertise_level_new = CASE 
        WHEN expertise_level = 'Professional' THEN 'Professional'::expertise_level
        WHEN expertise_level = 'Advanced' THEN 'Advanced'::expertise_level
        WHEN expertise_level = 'Intermediate' THEN 'Intermediate'::expertise_level
        WHEN expertise_level = 'Beginner' THEN 'Beginner'::expertise_level
        ELSE NULL
    END;

-- Supprimer les anciennes colonnes
ALTER TABLE users DROP COLUMN account_type;
ALTER TABLE users DROP COLUMN subscription_status;
ALTER TABLE users DROP COLUMN expertise_level;

-- Renommer les nouvelles colonnes
ALTER TABLE users RENAME COLUMN account_type_new TO account_type;
ALTER TABLE users RENAME COLUMN subscription_status_new TO subscription_status;
ALTER TABLE users RENAME COLUMN expertise_level_new TO expertise_level;

-- ========================================
-- 🔧 ÉTAPE 3: Vérification finale
-- ========================================

SELECT '=== VÉRIFICATION DES ENUMS CORRIGÉS ===' as status;

-- Vérifier les nouvelles valeurs d'enum
SELECT 'Enum account_type:' as info;
SELECT unnest(enum_range(NULL::account_type)) as values;

SELECT 'Enum subscription_status:' as info;
SELECT unnest(enum_range(NULL::subscription_status)) as values;

SELECT 'Enum expertise_level:' as info;
SELECT unnest(enum_range(NULL::expertise_level)) as values;

-- Vérifier les utilisateurs existants
SELECT 'Utilisateurs après correction:' as info;
SELECT username, email, account_type, subscription_status, expertise_level
FROM users;

SELECT '✅ ENUMS CORRIGÉS - L''APPLICATION PEUT MAINTENANT SE CONNECTER !' as final_status;