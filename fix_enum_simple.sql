-- ========================================
-- CORRECTION SIMPLE DES ENUMS SUPABASE
-- ========================================
-- Script simplifié pour éviter les erreurs Supabase

-- Étape 1: Supprimer l'utilisateur existant
DELETE FROM user_preferences WHERE user_id IN (SELECT id FROM users WHERE email = 'admin@subexplore.com');
DELETE FROM users WHERE email = 'admin@subexplore.com';

-- Étape 2: Supprimer tous les types enum existants
DROP TYPE IF EXISTS account_type CASCADE;
DROP TYPE IF EXISTS subscription_status CASCADE;
DROP TYPE IF EXISTS expertise_level CASCADE;
DROP TYPE IF EXISTS activity_category CASCADE;

-- Étape 3: Recréer les enums avec les valeurs exactes du code C#
CREATE TYPE account_type AS ENUM (
    'Standard',
    'ExpertModerator', 
    'VerifiedProfessional',
    'Administrator'
);

CREATE TYPE subscription_status AS ENUM (
    'Free',
    'Premium', 
    'PremiumPlus',
    'Suspended'
);

CREATE TYPE expertise_level AS ENUM (
    'Beginner',
    'Intermediate',
    'Advanced', 
    'Expert',
    'Professional'
);

CREATE TYPE activity_category AS ENUM (
    'Activity',
    'Structure',
    'Shop',
    'Other'
);

-- Étape 4: Recréer l'utilisateur admin avec les bonnes valeurs
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
    'PremiumPlus'::subscription_status,
    true
);

-- Étape 5: Recréer les préférences admin
INSERT INTO user_preferences (user_id, theme, language) 
SELECT id, 'Dark', 'fr' FROM users WHERE username = 'admin';

-- Vérification finale
SELECT 'Utilisateur admin créé:' as info;
SELECT username, email, account_type, subscription_status FROM users WHERE email = 'admin@subexplore.com';

SELECT 'Types enum disponibles:' as info;
SELECT unnest(enum_range(NULL::account_type)) as account_type_values;
SELECT unnest(enum_range(NULL::subscription_status)) as subscription_status_values;