-- ========================================
-- CORRECTION MINIMALE - TABLE USERS SEULEMENT
-- ========================================

-- Supprimer les dépendances de users
DROP TABLE IF EXISTS user_preferences CASCADE;
DROP TABLE IF EXISTS email_verification_tokens CASCADE;
DROP TABLE IF EXISTS password_reset_tokens CASCADE;
DROP TABLE IF EXISTS revoked_tokens CASCADE;
DROP TABLE IF EXISTS user_favorite_spots CASCADE;

-- Supprimer table users
DROP TABLE IF EXISTS users CASCADE;

-- Recréer les enums corrects
DROP TYPE IF EXISTS account_type CASCADE;
DROP TYPE IF EXISTS subscription_status CASCADE;
DROP TYPE IF EXISTS expertise_level CASCADE;

CREATE TYPE account_type AS ENUM ('Standard', 'ExpertModerator', 'VerifiedProfessional', 'Administrator');
CREATE TYPE subscription_status AS ENUM ('Free', 'Premium', 'PremiumPlus', 'Suspended');  
CREATE TYPE expertise_level AS ENUM ('Beginner', 'Intermediate', 'Advanced', 'Expert', 'Professional');

-- Recréer table users
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

-- Recréer user_preferences
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

-- Créer admin utilisateur
INSERT INTO users (email, password_hash, username, first_name, last_name, account_type, subscription_status, is_email_confirmed) 
VALUES ('admin@subexplore.com', '$2a$11$dummyhash.for.testing.purposes.only', 'admin', 'Admin', 'SubExplore', 'Administrator', 'PremiumPlus', true);

INSERT INTO user_preferences (user_id, theme, language) 
SELECT id, 'Dark', 'fr' FROM users WHERE username = 'admin';

SELECT 'Correction terminée!' as status;