-- ========================================
-- SCRIPT CORRIGÉ POUR AJOUTER DES BOUTIQUES
-- Résout le problème de foreign key constraint
-- ========================================

-- 1. Vérifier les utilisateurs disponibles dans public.users (pas auth.users)
SELECT 'Utilisateurs dans public.users:' as info, COUNT(*) as count FROM public.users;
SELECT 'Utilisateurs dans auth.users:' as info, COUNT(*) as count FROM auth.users;

-- 2. Créer un utilisateur test dans public.users s'il n'existe pas
INSERT INTO public.users (
    id,
    email,
    password_hash,
    first_name,
    last_name,
    account_type,
    subscription_status,
    is_email_confirmed,
    created_at,
    updated_at
)
SELECT 
    gen_random_uuid(),
    'boutique@subexplore.com',
    'hashed_password_placeholder',
    'Boutique',
    'Admin',
    'Standard',
    'Free',
    true,
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM public.users WHERE email = 'boutique@subexplore.com'
);

-- 3. AJOUTER DES BOUTIQUES avec un creator_id valide
INSERT INTO spots (
    id,
    creator_id,
    name,
    description,
    latitude,
    longitude,
    difficulty_level,
    type_id,
    required_equipment,
    safety_notes,
    best_conditions,
    created_at,
    validation_status,
    last_safety_review,
    safety_flags,
    max_depth,
    current_strength,
    has_mooring,
    bottom_type
)
SELECT 
    gen_random_uuid(),
    (SELECT id FROM public.users ORDER BY created_at LIMIT 1),
    'AquaTech Diving Store',
    'Magasin spécialisé dans la vente et location d''équipements de plongée. Large gamme de détendeurs, combinaisons, masques et accessoires.',
    43.2965,
    5.3698,
    1,
    (SELECT id FROM spot_types WHERE name = 'Boutique' LIMIT 1),
    'Aucun équipement requis',
    'Magasin en surface, aucun risque',
    'Ouvert toute l''année',
    NOW(),
    2,
    NOW(),
    '{"commercial": true, "parking_available": true}',
    0,
    0,
    false,
    'commercial'
WHERE NOT EXISTS (
    SELECT 1 FROM spots WHERE name = 'AquaTech Diving Store'
);

-- 4. Ajouter une deuxième boutique
INSERT INTO spots (
    id, creator_id, name, description, latitude, longitude, 
    difficulty_level, type_id, required_equipment, safety_notes, 
    best_conditions, created_at, validation_status, last_safety_review, 
    safety_flags, max_depth, current_strength, has_mooring, bottom_type
)
SELECT 
    gen_random_uuid(),
    (SELECT id FROM public.users ORDER BY created_at LIMIT 1),
    'Diving Pro Shop',
    'Boutique spécialisée dans les équipements de plongée professionnelle.',
    43.3047, 5.3719, 1,
    (SELECT id FROM spot_types WHERE name = 'Boutique' LIMIT 1),
    'Aucun', 'Accès libre', 'Lundi-Samedi 9h-18h',
    NOW(), 2, NOW(),
    '{"technical_diving": true, "repair": true}',
    0, 0, false, 'commercial'
WHERE NOT EXISTS (SELECT 1 FROM spots WHERE name = 'Diving Pro Shop');

-- 5. VÉRIFICATION FINALE
SELECT 'RÉSULTATS:' as section;
SELECT 'Utilisateurs public.users:' as info, COUNT(*) as count FROM public.users;
SELECT 'Boutiques créées:' as info, COUNT(*) as count FROM spots s 
JOIN spot_types st ON s.type_id = st.id 
WHERE st.name = 'Boutique';

-- 6. Lister les boutiques créées
SELECT 
    s.name,
    s.latitude,
    s.longitude,
    u.email as creator_email
FROM spots s
JOIN spot_types st ON s.type_id = st.id
JOIN public.users u ON s.creator_id = u.id
WHERE st.name = 'Boutique';