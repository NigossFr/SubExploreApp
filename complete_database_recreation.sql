-- ========================================
-- RECRÉATION COMPLÈTE ET DÉFINITIVE DE LA DB
-- ========================================
-- Solution complète pour corriger tous les problèmes d'enum

-- Étape 1: Supprimer TOUTES les tables dans l'ordre des dépendances
DROP TABLE IF EXISTS user_preferences CASCADE;
DROP TABLE IF EXISTS email_verification_tokens CASCADE;
DROP TABLE IF EXISTS password_reset_tokens CASCADE;
DROP TABLE IF EXISTS revoked_tokens CASCADE;
DROP TABLE IF EXISTS user_favorite_spots CASCADE;
DROP TABLE IF EXISTS spot_media CASCADE;
DROP TABLE IF EXISTS spots CASCADE;
DROP TABLE IF EXISTS spot_types CASCADE;
DROP TABLE IF EXISTS users CASCADE;

-- Étape 2: Supprimer tous les types enum
DROP TYPE IF EXISTS account_type CASCADE;
DROP TYPE IF EXISTS subscription_status CASCADE;
DROP TYPE IF EXISTS expertise_level CASCADE;
DROP TYPE IF EXISTS activity_category CASCADE;

-- Étape 3: Recréer les enums avec les valeurs EXACTES du C#
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

-- Étape 4: Recréer TOUTES les tables
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

CREATE TABLE spots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    creator_id UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    latitude DECIMAL(10,8) NOT NULL,
    longitude DECIMAL(11,8) NOT NULL,
    difficulty_level INTEGER DEFAULT 1,
    type_id UUID NOT NULL REFERENCES spot_types(id) ON DELETE RESTRICT,
    required_equipment TEXT,
    safety_notes TEXT,
    best_conditions TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    validation_status INTEGER DEFAULT 0,
    last_safety_review TIMESTAMP WITH TIME ZONE,
    safety_flags JSONB DEFAULT '[]'::jsonb,
    max_depth DECIMAL(5,2),
    current_strength INTEGER DEFAULT 0,
    has_mooring BOOLEAN DEFAULT FALSE,
    bottom_type VARCHAR(50)
);

CREATE TABLE spot_media (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    spot_id UUID NOT NULL REFERENCES spots(id) ON DELETE CASCADE,
    media_type VARCHAR(20) NOT NULL DEFAULT 'Photo',
    media_url TEXT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    status VARCHAR(20) DEFAULT 'Active',
    caption TEXT,
    is_primary BOOLEAN DEFAULT FALSE,
    width INTEGER,
    height INTEGER,
    file_size BIGINT,
    content_type VARCHAR(100)
);

CREATE TABLE user_favorite_spots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    spot_id UUID NOT NULL REFERENCES spots(id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    notes TEXT,
    priority INTEGER DEFAULT 1,
    notification_enabled BOOLEAN DEFAULT TRUE,
    UNIQUE(user_id, spot_id)
);

CREATE TABLE email_verification_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) UNIQUE NOT NULL,
    email VARCHAR(255) NOT NULL,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    is_used BOOLEAN NOT NULL DEFAULT FALSE,
    used_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    attempt_count INTEGER DEFAULT 0,
    max_attempts INTEGER DEFAULT 5
);

CREATE TABLE password_reset_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) UNIQUE NOT NULL,
    email VARCHAR(255) NOT NULL,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    is_used BOOLEAN NOT NULL DEFAULT FALSE,
    used_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    attempt_count INTEGER DEFAULT 0,
    max_attempts INTEGER DEFAULT 3,
    reset_reason VARCHAR(255)
);

CREATE TABLE revoked_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    token_hash VARCHAR(255) UNIQUE NOT NULL,
    token_type VARCHAR(50) NOT NULL,
    user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    revoked_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    revocation_reason VARCHAR(255)
);

-- Étape 5: Recréer les index
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_spots_location ON spots(latitude, longitude);
CREATE INDEX idx_spots_type ON spots(type_id);
CREATE INDEX idx_spots_creator ON spots(creator_id);
CREATE INDEX idx_user_favorites_user ON user_favorite_spots(user_id);
CREATE INDEX idx_user_favorites_spot ON user_favorite_spots(spot_id);
CREATE INDEX idx_spot_media_spot ON spot_media(spot_id);
CREATE INDEX idx_email_tokens_hash ON email_verification_tokens(token_hash);
CREATE INDEX idx_password_tokens_hash ON password_reset_tokens(token_hash);
CREATE INDEX idx_revoked_tokens_hash ON revoked_tokens(token_hash);

-- Étape 6: Insérer l'utilisateur admin avec les BONNES valeurs
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
    'Administrator'::account_type,
    'Free'::subscription_status,
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

-- Étape 9: Vérification finale complète
SELECT '=== VÉRIFICATION FINALE COMPLÈTE ===' as status;

SELECT 'Tables créées:' as info;
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
ORDER BY table_name;

SELECT 'Enums créés:' as info;
SELECT typname as enum_name, array_agg(enumlabel ORDER BY enumsortorder) as values
FROM pg_enum 
JOIN pg_type ON pg_enum.enumtypid = pg_type.oid
WHERE pg_type.typname IN ('activity_category', 'account_type', 'subscription_status', 'expertise_level')
GROUP BY typname;

SELECT 'Utilisateur admin:' as info;
SELECT username, email, account_type, subscription_status FROM users;

SELECT 'Spot types:' as info;
SELECT name, category FROM spot_types ORDER BY category;

SELECT '🎉 BASE DE DONNÉES COMPLÈTEMENT RECRÉE - TESTEZ MAINTENANT !' as final_status;