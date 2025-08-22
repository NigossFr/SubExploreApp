-- ========================================
-- SCRIPT FINAL POUR AJOUTER UNE BOUTIQUE
-- Adapté à la structure réelle de votre base
-- ========================================

-- 1. AJOUTER LE TYPE "Boutique" avec les bonnes colonnes
INSERT INTO spot_types (
    id,
    name,
    icon_path,
    color_code,
    requires_expert_validation,
    validation_criteria,
    category,
    description,
    is_active,
    created_at,
    updated_at
)
SELECT 
    gen_random_uuid(),
    'Boutique',
    'shop.svg',
    '#FF6B35',
    false,
    '{"commercial": true, "equipment_sales": true, "location_verified": true}',
    'shop',
    'Magasin de vente et location d''équipements de plongée',
    true,
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM spot_types WHERE name = 'Boutique'
);

-- 2. Vérifier la structure de la table spots
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'spots' AND table_schema = 'public'
ORDER BY ordinal_position;

-- 3. AJOUTER UNE BOUTIQUE DE TEST (sera adapté après avoir vu la structure spots)
-- Version de base pour commencer
INSERT INTO spots (
    id,
    name,
    description,
    latitude,
    longitude,
    spot_type_id,
    created_at,
    updated_at
)
SELECT 
    gen_random_uuid(),
    'AquaTech Diving Store',
    'Magasin spécialisé dans la vente et location d''équipements de plongée. Large gamme de détendeurs, combinaisons, masques et accessoires. Service de maintenance et réparation disponible.',
    43.2965,
    5.3698,
    (SELECT id FROM spot_types WHERE name = 'Boutique' LIMIT 1),
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM spots WHERE name = 'AquaTech Diving Store'
);

-- 4. VÉRIFICATION
SELECT 'spot_types créés:' as info, COUNT(*) as count FROM spot_types;
SELECT 'spots créés:' as info, COUNT(*) as count FROM spots;
SELECT 'boutiques créées:' as info, COUNT(*) as count FROM spots s 
JOIN spot_types st ON s.spot_type_id = st.id 
WHERE st.name = 'Boutique';