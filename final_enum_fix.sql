-- ========================================
-- SOLUTION FINALE ENUM MAPPING SUPABASE
-- ========================================
-- Fix définitif pour correspondre exactement au C#

-- Étape 1: Vérifier les valeurs actuelles
SELECT 'Valeurs actuelles dans users:' as check_type;
SELECT DISTINCT account_type FROM users;

SELECT 'Valeurs actuelles dans spot_types:' as check_type;
SELECT DISTINCT category FROM spot_types;

-- Étape 2: Supprimer les utilisateurs existants temporairement
DELETE FROM user_preferences;
DELETE FROM users;

-- Étape 3: Supprimer tous les spot_types existants
DELETE FROM spot_types;

-- Étape 4: Supprimer et recréer TOUS les enums avec les EXACTES valeurs C#
DROP TYPE IF EXISTS account_type CASCADE;
DROP TYPE IF EXISTS subscription_status CASCADE;
DROP TYPE IF EXISTS expertise_level CASCADE;
DROP TYPE IF EXISTS activity_category CASCADE;

-- Recréer avec les valeurs exactes du C# UserEnums.cs
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

-- Étape 5: Recréer l'utilisateur admin avec les BONNES valeurs
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
    'Administrator'::account_type,  -- Utilise le nouveau enum
    'Free'::subscription_status,    -- Utilise Free au lieu de PremiumPlus
    true
);

-- Étape 6: Recréer les préférences
INSERT INTO user_preferences (user_id, theme, language) 
SELECT id, 'Dark', 'fr' FROM users WHERE username = 'admin';

-- Étape 7: Recréer les spot_types avec les bonnes valeurs
INSERT INTO spot_types (name, icon_path, color_code, category, is_active, description) VALUES
('Plongée bouteille', 'marker_diving.png', '#0077BE', 'Activity'::activity_category, true, 'Sites adaptés à la plongée avec bouteille'),
('Apnée', 'marker_freediving.png', '#4A90E2', 'Activity'::activity_category, true, 'Sites adaptés à la plongée en apnée'),
('Snorkeling', 'marker_snorkeling.png', '#00CC66', 'Activity'::activity_category, true, 'Sites adaptés au snorkeling'),
('Club de plongée', 'marker_club.png', '#8E44AD', 'Structure'::activity_category, true, 'Club ou association de plongée'),
('Magasin de plongée', 'marker_shop.png', '#F39C12', 'Shop'::activity_category, true, 'Boutique spécialisée équipement plongée'),
('Épave', 'marker_wreck.png', '#34495E', 'Other'::activity_category, true, 'Site d''épave sous-marine');

-- Étape 8: Vérification finale
SELECT '=== VÉRIFICATION FINALE ===' as status;

SELECT 'Enum account_type créé:' as info;
SELECT unnest(enum_range(NULL::account_type)) as values;

SELECT 'Enum activity_category créé:' as info;
SELECT unnest(enum_range(NULL::activity_category)) as values;

SELECT 'Utilisateur admin créé:' as info;
SELECT username, email, account_type, subscription_status FROM users;

SELECT 'Spot types créés:' as info;
SELECT name, category FROM spot_types;

SELECT '✅ CORRECTION TERMINÉE - TESTEZ MAINTENANT VOTRE APPLICATION !' as final_status;