-- ========================================
-- SCRIPT FINAL POUR AJOUTER DES BOUTIQUES
-- Adapté à la structure réelle des tables
-- ========================================

-- 1. Vérifier que le type Boutique existe
SELECT 'Type Boutique existant:' as info, COUNT(*) as count FROM spot_types WHERE name = 'Boutique';

-- 2. Obtenir un creator_id (utilisateur existant)
SELECT 'Utilisateurs disponibles:' as info, COUNT(*) as count FROM auth.users;

-- 3. AJOUTER DES BOUTIQUES DE TEST avec la bonne structure
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
    (SELECT id FROM auth.users ORDER BY created_at LIMIT 1),
    'AquaTech Diving Store',
    'Magasin spécialisé dans la vente et location d''équipements de plongée. Large gamme de détendeurs, combinaisons, masques et accessoires. Service de maintenance et réparation disponible.',
    43.2965,
    5.3698,
    1, -- Difficulté faible pour une boutique
    (SELECT id FROM spot_types WHERE name = 'Boutique' LIMIT 1),
    'Aucun équipement spécial requis pour visiter le magasin',
    'Magasin en surface, aucun risque particulier',
    'Ouvert toute l''année, meilleur moment pendant les heures d''ouverture',
    NOW(),
    2, -- Status validé (à ajuster selon vos valeurs)
    NOW(),
    '{"commercial": true, "parking_available": true, "wheelchair_accessible": true}',
    0, -- Pas de profondeur pour une boutique
    0, -- Pas de courant
    false, -- Pas de mouillage
    'commercial' -- Type de fond
WHERE NOT EXISTS (
    SELECT 1 FROM spots WHERE name = 'AquaTech Diving Store'
);

-- 4. AJOUTER UNE DEUXIÈME BOUTIQUE
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
    (SELECT id FROM auth.users ORDER BY created_at LIMIT 1),
    'Diving Pro Shop',
    'Boutique spécialisée dans les équipements de plongée professionnelle. Formation Nitrox, réparation de détendeurs, vente de matériel technique.',
    43.3047,
    5.3719,
    1,
    (SELECT id FROM spot_types WHERE name = 'Boutique' LIMIT 1),
    'Aucun équipement requis',
    'Accès libre pendant les heures d''ouverture',
    'Lundi-Samedi 9h-18h, fermé dimanche',
    NOW(),
    2,
    NOW(),
    '{"technical_diving": true, "nitrox_training": true, "equipment_repair": true}',
    0,
    0,
    false,
    'commercial'
WHERE NOT EXISTS (
    SELECT 1 FROM spots WHERE name = 'Diving Pro Shop'
);

-- 5. AJOUTER UNE TROISIÈME BOUTIQUE À NICE
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
    (SELECT id FROM auth.users ORDER BY created_at LIMIT 1),
    'Azur Diving Equipment',
    'Magasin d''équipements de plongée situé sur la Côte d''Azur. Spécialiste des combinaisons sur mesure et du matériel photo sous-marine.',
    43.7102,
    7.2620,
    1,
    (SELECT id FROM spot_types WHERE name = 'Boutique' LIMIT 1),
    'Aucun',
    'Magasin en centre-ville, accès facile',
    'Ouvert toute l''année, horaires étendus en été',
    NOW(),
    2,
    NOW(),
    '{"custom_wetsuits": true, "underwater_photo": true, "repair_service": true}',
    0,
    0,
    false,
    'commercial'
WHERE NOT EXISTS (
    SELECT 1 FROM spots WHERE name = 'Azur Diving Equipment'
);

-- 6. VÉRIFICATION FINALE
SELECT 'RÉSULTATS FINAUX:' as section;
SELECT 'Types de spots total:' as info, COUNT(*) as count FROM spot_types;
SELECT 'Boutiques type créées:' as info, COUNT(*) as count FROM spot_types WHERE name = 'Boutique';
SELECT 'Spots total:' as info, COUNT(*) as count FROM spots;
SELECT 'Boutiques spots créées:' as info, COUNT(*) as count FROM spots s 
JOIN spot_types st ON s.type_id = st.id 
WHERE st.name = 'Boutique';

-- 7. LISTER LES BOUTIQUES CRÉÉES
SELECT 
    s.name as boutique_name,
    s.description,
    s.latitude,
    s.longitude,
    st.name as type_name
FROM spots s
JOIN spot_types st ON s.type_id = st.id
WHERE st.name = 'Boutique'
ORDER BY s.created_at;