-- ========================================
-- SCRIPT POUR AJOUTER UNE BOUTIQUE DE TEST
-- Copier-coller directement dans l'éditeur SQL
-- ========================================

-- 1. AJOUTER LE TYPE "Boutique" s'il n'existe pas
INSERT INTO spot_types (
    id,
    name,
    icon_name,
    color,
    is_active,
    additional_fields,
    category,
    description,
    is_validated,
    created_at,
    updated_at
)
SELECT 
    gen_random_uuid(),
    'Boutique',
    'shop.svg',
    '#FF6B35',
    true,
    '{"commercial": true, "equipment_sales": true}',
    'shop',
    'Magasin de vente et location d''équipements de plongée',
    true,
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM spot_types WHERE name = 'Boutique'
);

-- 2. OBTENIR UN UTILISATEUR EXISTANT (premier de la liste)
-- Si aucun utilisateur n'existe, en créer un temporaire
INSERT INTO users (
    id,
    email,
    password_hash,
    first_name,
    last_name,
    account_type,
    subscription_status,
    is_email_confirmed,
    created_at
)
SELECT 
    gen_random_uuid(),
    'test@subexplore.com',
    'hashed_password',
    'Test',
    'User',
    'Standard',
    'Free',
    true,
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM users WHERE email = 'test@subexplore.com'
);

-- 3. AJOUTER UNE BOUTIQUE DE TEST
INSERT INTO spots (
    id,
    name,
    description,
    latitude,
    longitude,
    best_time_to_visit,
    access_instructions,
    additional_info,
    status,
    created_by,
    spot_type_id,
    is_validated,
    validation_score,
    created_at,
    updated_at
)
SELECT 
    gen_random_uuid(),
    'AquaTech Diving Store',
    'Magasin spécialisé dans la vente et location d''équipements de plongée. Large gamme de détendeurs, combinaisons, masques et accessoires.',
    43.2965,
    5.3698,
    'Toute l''année',
    'Situé en centre-ville, proche du port. Parking disponible.',
    '{"horaires": "Lun-Sam 9h-18h", "services": ["vente", "location", "réparation"], "contact": "04 91 XX XX XX"}',
    'validated',
    (SELECT id FROM users LIMIT 1),
    (SELECT id FROM spot_types WHERE name = 'Boutique' LIMIT 1),
    true,
    100,
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM spots WHERE name = 'AquaTech Diving Store'
);

-- 4. VÉRIFICATION DES DONNÉES
SELECT 'Types de spots créés:' as info, COUNT(*) as count FROM spot_types;
SELECT 'Spots créés:' as info, COUNT(*) as count FROM spots;
SELECT 'Boutiques créées:' as info, COUNT(*) as count FROM spots s 
JOIN spot_types st ON s.spot_type_id = st.id 
WHERE st.name = 'Boutique';