-- ========================================
-- CORRECTION CRITIQUE: Mapping enum ActivityCategory
-- ========================================
-- Ce script corrige le problème d'enum entre la DB Supabase et le code C#

-- 🔧 SOLUTION 1: Recréer l'enum activity_category avec les bonnes valeurs
-- ========================================

BEGIN;

-- Étape 1: Sauvegarder les données existantes si elles existent
DO $$
BEGIN
    -- Vérifier si la table spot_types existe
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'spot_types') THEN
        -- Créer une table temporaire pour sauvegarder les données
        CREATE TABLE IF NOT EXISTS temp_spot_types_backup AS 
        SELECT *, category::text as old_category_text 
        FROM spot_types;
        
        RAISE NOTICE 'Données sauvegardées dans temp_spot_types_backup';
    END IF;
END $$;

-- Étape 2: Supprimer l'ancien enum s'il existe
DROP TYPE IF EXISTS activity_category CASCADE;

-- Étape 3: Créer le nouvel enum avec les valeurs C# correctes
CREATE TYPE activity_category AS ENUM (
    'Activity',    -- 0: Toutes les activités sous-marines (remplace diving, freediving, snorkeling, underwater_photography)
    'Structure',   -- 1: Clubs, centres, bases fédérales  
    'Shop',        -- 2: Boutiques et magasins
    'Other'        -- 3: Autres types
);

-- Étape 4: Créer/Recréer les autres enums nécessaires
DROP TYPE IF EXISTS account_type CASCADE;
CREATE TYPE account_type AS ENUM ('Standard', 'Premium', 'Administrator');

DROP TYPE IF EXISTS subscription_status CASCADE;
CREATE TYPE subscription_status AS ENUM ('Free', 'Premium', 'Enterprise');

DROP TYPE IF EXISTS expertise_level CASCADE;
CREATE TYPE expertise_level AS ENUM ('Beginner', 'Intermediate', 'Advanced', 'Professional');

-- ========================================
-- 🔧 SOLUTION 2: Recréer les tables avec les bons types
-- ========================================

-- Créer la table spot_types avec le bon schéma
CREATE TABLE IF NOT EXISTS spot_types (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    icon_path VARCHAR(255),
    color_code VARCHAR(7) NOT NULL DEFAULT '#000000',
    requires_expert_validation BOOLEAN NOT NULL DEFAULT FALSE,
    validation_criteria JSONB,
    category activity_category NOT NULL DEFAULT 'Activity',
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- 🔧 SOLUTION 3: Insérer les données de base
-- ========================================

-- Supprimer les anciennes données pour éviter les conflits
TRUNCATE TABLE spot_types;

-- Insérer les types de spots standard avec la nouvelle classification
INSERT INTO spot_types (name, icon_path, color_code, requires_expert_validation, description, category, is_active, validation_criteria) VALUES
-- Activités sous-marines (ancien diving/freediving/snorkeling regroupé)
(
    'Plongée bouteille',
    'marker_diving.png', 
    '#0077BE',
    true,
    'Sites adaptés à la plongée avec bouteille',
    'Activity'::activity_category,
    true,
    '{"min_depth": 5, "max_depth": 60, "required_certifications": ["Open Water"], "safety_equipment": ["BCD", "Détendeur", "Masque", "Palmes"]}'::jsonb
),
(
    'Apnée',
    'marker_freediving.png',
    '#4A90E2', 
    true,
    'Sites adaptés à la plongée en apnée',
    'Activity'::activity_category,
    true,
    '{"min_depth": 0, "max_depth": 30, "safety_equipment": ["Masque", "Palmes", "Tuba"], "buddy_system": true}'::jsonb
),
(
    'Snorkeling',
    'marker_snorkeling.png',
    '#00CC66',
    false,
    'Sites adaptés au snorkeling en surface',
    'Activity'::activity_category,
    true,
    '{"max_depth": 5, "safety_equipment": ["Masque", "Palmes", "Tuba"]}'::jsonb
),
(
    'Photo sous-marine',
    'marker_photography.png',
    '#FF6B35',
    false,
    'Sites adaptés à la photographie sous-marine',
    'Activity'::activity_category,
    true,
    '{"visibility_requirement": "good", "lighting": "natural"}'::jsonb
),

-- Structures (centres, clubs)
(
    'Club de plongée',
    'marker_club.png',
    '#8E44AD',
    false,
    'Club ou association de plongée',
    'Structure'::activity_category,
    true,
    '{"services": ["formation", "location_materiel", "sorties_organisees"]}'::jsonb
),
(
    'Centre de plongée',
    'marker_center.png',
    '#2ECC71',
    false,
    'Centre commercial de plongée',
    'Structure'::activity_category,
    true,
    '{"services": ["formation", "location_materiel", "sorties_organisees", "hotel"]}'::jsonb
),

-- Boutiques
(
    'Magasin de plongée',
    'marker_shop.png',
    '#F39C12',
    false,
    'Boutique spécialisée équipement plongée',
    'Shop'::activity_category,
    true,
    '{"services": ["vente", "location", "reparation"]}'::jsonb
),

-- Autres
(
    'Épave',
    'marker_wreck.png',
    '#34495E',
    true,
    'Site d\'épave sous-marine',
    'Other'::activity_category,
    true,
    '{"historical_info": true, "access_restrictions": "possible"}'::jsonb
);

-- ========================================
-- 🔧 SOLUTION 4: Créer les autres tables essentielles
-- ========================================

-- Table users 
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    username VARCHAR(30) UNIQUE,
    first_name VARCHAR(50) NOT NULL,
    last_name VARCHAR(50) NOT NULL,
    avatar_url TEXT,
    account_type account_type DEFAULT 'Standard',
    subscription_status subscription_status DEFAULT 'Free',
    expertise_level expertise_level,
    certifications JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    last_login TIMESTAMP WITH TIME ZONE,
    is_email_confirmed BOOLEAN DEFAULT FALSE,
    moderator_specialization INTEGER DEFAULT 0,
    moderator_status INTEGER DEFAULT 0,
    permissions INTEGER DEFAULT 1,
    moderator_since TIMESTAMP WITH TIME ZONE,
    organization_id UUID
);

-- Table spots
CREATE TABLE IF NOT EXISTS spots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    creator_id UUID NOT NULL REFERENCES users(id),
    name VARCHAR(200) NOT NULL,
    description TEXT,
    latitude DECIMAL(10,8) NOT NULL,
    longitude DECIMAL(11,8) NOT NULL,
    difficulty_level INTEGER DEFAULT 1,
    type_id UUID NOT NULL REFERENCES spot_types(id),
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

-- Index pour optimiser les recherches
CREATE INDEX IF NOT EXISTS idx_spots_location ON spots(latitude, longitude);
CREATE INDEX IF NOT EXISTS idx_spots_type ON spots(type_id);
CREATE INDEX IF NOT EXISTS idx_spots_creator ON spots(creator_id);

-- ========================================
-- Validation des données
-- ========================================

-- Vérifier que tout s'est bien passé
SELECT 'Verification des enums:' as status;
SELECT enum_range(NULL::activity_category) as activity_category_values;
SELECT enum_range(NULL::account_type) as account_type_values;
SELECT enum_range(NULL::subscription_status) as subscription_status_values;
SELECT enum_range(NULL::expertise_level) as expertise_level_values;

-- Vérifier les données insérées
SELECT 'Verification des spot_types:' as status;
SELECT name, category, is_active FROM spot_types ORDER BY category, name;

COMMIT;

-- ========================================
-- Instructions post-migration
-- ========================================

SELECT 'MIGRATION TERMINÉE!' as status;
SELECT 'Les enums ont été recréés avec les valeurs C# correctes' as info;
SELECT 'Les tables sont maintenant compatibles avec votre application MAUI' as info;