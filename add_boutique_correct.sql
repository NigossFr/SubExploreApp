-- ========================================
-- SCRIPT POUR AJOUTER UNE BOUTIQUE (VERSION CORRIGÉE)
-- ========================================

-- 1. D'abord, vérifier la structure exacte des tables
SELECT 'Structure de spot_types:' as info;
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'spot_types' AND table_schema = 'public'
ORDER BY ordinal_position;

SELECT 'Structure de spots:' as info;
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'spots' AND table_schema = 'public'
ORDER BY ordinal_position;

-- 2. AJOUTER LE TYPE "Boutique" avec seulement les colonnes qui existent
-- Version simplifiée basée sur les colonnes essentielles
INSERT INTO spot_types (
    id,
    name,
    color,
    category,
    description,
    is_validated,
    created_at,
    updated_at
)
SELECT 
    gen_random_uuid(),
    'Boutique',
    '#FF6B35',
    'shop',
    'Magasin de vente et location d''équipements de plongée',
    true,
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM spot_types WHERE name = 'Boutique'
);

-- 3. AJOUTER UNE BOUTIQUE DE TEST (version simplifiée)
INSERT INTO spots (
    id,
    name,
    description,
    latitude,
    longitude,
    status,
    created_by,
    spot_type_id,
    is_validated,
    created_at,
    updated_at
)
SELECT 
    gen_random_uuid(),
    'AquaTech Diving Store',
    'Magasin spécialisé dans la vente et location d''équipements de plongée.',
    43.2965,
    5.3698,
    'validated',
    (SELECT id FROM auth.users ORDER BY created_at LIMIT 1),
    (SELECT id FROM spot_types WHERE name = 'Boutique' LIMIT 1),
    true,
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM spots WHERE name = 'AquaTech Diving Store'
);

-- 4. VÉRIFICATION FINALE
SELECT 'Résultats:' as info;
SELECT 'Types de spots:' as type, COUNT(*) as count FROM spot_types;
SELECT 'Spots total:' as type, COUNT(*) as count FROM spots;
SELECT 'Boutiques:' as type, COUNT(*) as count FROM spots s 
JOIN spot_types st ON s.spot_type_id = st.id 
WHERE st.name = 'Boutique';