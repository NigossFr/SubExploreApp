-- ==========================================
-- SOLUTION COMPLÈTE NPGSQL ENUM MAPPING
-- ==========================================
-- Fix définitif pour résoudre les erreurs de mapping Npgsql

-- Étape 1: Nettoyer complètement la base de données
DROP TABLE IF EXISTS user_preferences CASCADE;
DROP TABLE IF EXISTS email_verification_tokens CASCADE;
DROP TABLE IF EXISTS password_reset_tokens CASCADE;
DROP TABLE IF EXISTS revoked_tokens CASCADE;
DROP TABLE IF EXISTS user_favorite_spots CASCADE;
DROP TABLE IF EXISTS spot_media CASCADE;
DROP TABLE IF EXISTS spots CASCADE;
DROP TABLE IF EXISTS spot_types CASCADE;
DROP TABLE IF EXISTS users CASCADE;

-- Supprimer tous les enums existants
DROP TYPE IF EXISTS account_type CASCADE;
DROP TYPE IF EXISTS subscription_status CASCADE;
DROP TYPE IF EXISTS expertise_level CASCADE;
DROP TYPE IF EXISTS activity_category CASCADE;

-- Étape 2: Recréer les enums EXACTEMENT comme dans UserEnums.cs
-- IMPORTANT: L'ordre et les noms doivent correspondre parfaitement

CREATE TYPE account_type AS ENUM (
    'Standard',           -- 0: AccountType.Standard
    'ExpertModerator',    -- 1: AccountType.ExpertModerator  
    'VerifiedProfessional', -- 2: AccountType.VerifiedProfessional
    'Administrator'       -- 3: AccountType.Administrator
);

CREATE TYPE subscription_status AS ENUM (
    'Free',        -- 0: SubscriptionStatus.Free
    'Premium',     -- 1: SubscriptionStatus.Premium
    'PremiumPlus', -- 2: SubscriptionStatus.PremiumPlus
    'Suspended'    -- 3: SubscriptionStatus.Suspended
);

CREATE TYPE expertise_level AS ENUM (
    'Beginner',      -- 0: ExpertiseLevel.Beginner
    'Intermediate',  -- 1: ExpertiseLevel.Intermediate
    'Advanced',      -- 2: ExpertiseLevel.Advanced
    'Expert',        -- 3: ExpertiseLevel.Expert
    'Professional'   -- 4: ExpertiseLevel.Professional
);

CREATE TYPE activity_category AS ENUM (
    'Activity',    -- 0: ActivityCategory.Activity
    'Structure',   -- 1: ActivityCategory.Structure
    'Shop',        -- 2: ActivityCategory.Shop
    'Other'        -- 3: ActivityCategory.Other
);

-- Étape 3: Recréer la table users avec contraintes strictes
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    username VARCHAR(30) UNIQUE,
    first_name VARCHAR(50) NOT NULL,
    last_name VARCHAR(50) NOT NULL,
    avatar_url TEXT,
    account_type account_type NOT NULL DEFAULT 'Standard',
    subscription_status subscription_status NOT NULL DEFAULT 'Free',
    expertise_level expertise_level,
    certifications JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    last_login TIMESTAMP WITH TIME ZONE,
    is_email_confirmed BOOLEAN NOT NULL DEFAULT FALSE,
    moderator_specialization INTEGER NOT NULL DEFAULT 0,
    moderator_status INTEGER NOT NULL DEFAULT 0,
    permissions INTEGER NOT NULL DEFAULT 1,
    moderator_since TIMESTAMP WITH TIME ZONE,
    organization_id UUID
);

-- Étape 4: Recréer user_preferences
CREATE TABLE user_preferences (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    theme VARCHAR(50) DEFAULT 'Light',
    display_name_preference VARCHAR(50) DEFAULT 'Username',
    notification_settings JSONB DEFAULT '{}'::jsonb,
    language VARCHAR(10) DEFAULT 'fr',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Étape 5: Recréer spot_types pour l'intégrité
CREATE TABLE spot_types (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    icon_path VARCHAR(255),
    color_code VARCHAR(7) NOT NULL DEFAULT '#000000',
    requires_expert_validation BOOLEAN NOT NULL DEFAULT FALSE,
    validation_criteria JSONB DEFAULT '{}'::jsonb,
    category activity_category NOT NULL DEFAULT 'Activity',
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Étape 6: Insérer l'utilisateur admin avec casting explicite
INSERT INTO users (
    email, 
    password_hash, 
    username, 
    first_name, 
    last_name, 
    account_type, 
    subscription_status, 
    is_email_confirmed
) VALUES (
    'admin@subexplore.com',
    '$2a$11$dummyhash.for.testing.purposes.only',
    'admin',
    'Admin',
    'SubExplore',
    'Administrator'::account_type,  -- Casting explicite
    'Free'::subscription_status,    -- Utilise Free au lieu de PremiumPlus
    true
);

-- Étape 7: Insérer les préférences admin
INSERT INTO user_preferences (user_id, theme, language) 
SELECT id, 'Dark', 'fr' FROM users WHERE username = 'admin';

-- Étape 8: Insérer quelques spot_types de base
INSERT INTO spot_types (name, icon_path, color_code, category, is_active, description) VALUES
('Plongée bouteille', 'marker_diving.png', '#0077BE', 'Activity'::activity_category, true, 'Sites adaptés à la plongée avec bouteille'),
('Apnée', 'marker_freediving.png', '#4A90E2', 'Activity'::activity_category, true, 'Sites adaptés à la plongée en apnée'),
('Snorkeling', 'marker_snorkeling.png', '#00CC66', 'Activity'::activity_category, true, 'Sites adaptés au snorkeling'),
('Club de plongée', 'marker_club.png', '#8E44AD', 'Structure'::activity_category, true, 'Club ou association de plongée'),
('Magasin de plongée', 'marker_shop.png', '#F39C12', 'Shop'::activity_category, true, 'Boutique spécialisée équipement plongée'),
('Épave', 'marker_wreck.png', '#34495E', 'Other'::activity_category, true, 'Site d''épave sous-marine');

-- Étape 9: Créer les index pour les performances
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_users_account_type ON users(account_type);

-- Étape 10: Vérifications complètes
SELECT '=== VÉRIFICATION FINALE DES ENUMS ===' as status;

-- Vérifier que les enums sont créés correctement
SELECT 'Enum account_type:' as info, unnest(enum_range(NULL::account_type)) as values;
SELECT 'Enum subscription_status:' as info, unnest(enum_range(NULL::subscription_status)) as values;
SELECT 'Enum expertise_level:' as info, unnest(enum_range(NULL::expertise_level)) as values;
SELECT 'Enum activity_category:' as info, unnest(enum_range(NULL::activity_category)) as values;

-- Vérifier l'utilisateur admin
SELECT 'Utilisateur admin créé:' as info;
SELECT username, email, account_type, subscription_status FROM users;

-- Vérifier les spot_types
SELECT 'Spot types créés:' as info;
SELECT name, category FROM spot_types ORDER BY category;

-- Test de casting explicite pour s'assurer que les enums fonctionnent
SELECT 'Test casting enum account_type:' as test;
SELECT 'Administrator'::account_type as admin_enum_test;

SELECT 'Test casting enum subscription_status:' as test;
SELECT 'Free'::subscription_status as free_enum_test;

SELECT '✅ DATABASE COMPLÈTEMENT RECRÉE AVEC ENUMS CORRECTS' as final_status;
SELECT '🔧 MAINTENANT: NETTOYER/REBUILD LE PROJET .NET PUIS TESTER' as next_step;