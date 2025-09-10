-- ========================================
-- CORRECTION BASE DE DONNÉES SUPABASE - SPOT TYPES
-- ========================================
-- Script pour corriger la base de données Supabase corrompue
-- Gère les contraintes de clés étrangères avant suppression

-- ÉTAPE 1: Analyser les références existantes
SELECT 
    st.id, 
    st.name,
    COUNT(s.id) as spots_count
FROM spot_types st
LEFT JOIN spots s ON s.type_id = st.id
WHERE st.name IN ('Plongée bouteille', 'Boutique', 'Magasin de plongée', 'Snorkeling', 'Cl')
GROUP BY st.id, st.name
ORDER BY st.name;

-- ÉTAPE 2: Créer d'abord les nouveaux types corrects (pour la migration)
INSERT INTO spot_types (
    id, name, icon_path, color_code, requires_expert_validation, 
    validation_criteria, category, description, is_active, created_at, updated_at
) VALUES 
(
    gen_random_uuid(),
    'Plongée bouteille NEW',  -- Nom temporaire pour éviter les conflits
    'marker_scuba.png',
    '#0077BE',
    true,
    '{"RequiredFields": ["DifficultyLevel", "SafetyNotes"], "MaxDepthRange": [0, 100]}',
    'Activity',
    'Sites de plongée avec bouteille (tous niveaux - récréative et technique)',
    true,
    NOW(),
    NOW()
),
(
    gen_random_uuid(),
    'Apnée NEW',
    'marker_freediving.png',
    '#4169E1',
    true,
    '{"RequiredFields": ["DifficultyLevel", "SafetyNotes"], "MaxDepthRange": [0, 50]}',
    'Activity',
    'Sites de plongée en apnée et freediving',
    true,
    NOW(),
    NOW()
),
(
    gen_random_uuid(),
    'Randonnée sous-marine NEW',
    'marker_snorkeling.png',
    '#87CEEB',
    false,
    '{"RequiredFields": ["DifficultyLevel", "SafetyNotes"], "MaxDepthRange": [0, 5]}',
    'Activity',
    'Sites de randonnée palmée et snorkeling',
    true,
    NOW(),
    NOW()
),
(
    gen_random_uuid(),
    'Photo sous-marine NEW',
    'marker_photography.png',
    '#5DADE2',
    false,
    '{"RequiredFields": ["DifficultyLevel"]}',
    'Activity',
    'Sites d''intérêt pour la photographie sous-marine',
    true,
    NOW(),
    NOW()
),
(
    gen_random_uuid(),
    'Clubs NEW',
    'marker_club.png',
    '#228B22',
    false,
    '{"RequiredFields": ["Description"]}',
    'Structure',
    'Clubs de plongée et associations',
    true,
    NOW(),
    NOW()
),
(
    gen_random_uuid(),
    'Professionnels NEW',
    'marker_professional.png',
    '#32CD32',
    false,
    '{"RequiredFields": ["Description", "ContactInfo"]}',
    'Structure',
    'Centres de plongée, instructeurs et guides professionnels',
    true,
    NOW(),
    NOW()
),
(
    gen_random_uuid(),
    'Bases fédérales NEW',
    'marker_federal.png',
    '#90EE90',
    false,
    '{"RequiredFields": ["Description", "ContactInfo"]}',
    'Structure',
    'Bases et installations officielles des fédérations',
    true,
    NOW(),
    NOW()
),
(
    gen_random_uuid(),
    'Boutiques NEW',
    'marker_shop.png',
    '#FFA500',
    false,
    '{"RequiredFields": ["Description", "ContactInfo"], "commercial": true}',
    'Shop',
    'Magasins et services commerciaux liés à la plongée',
    true,
    NOW(),
    NOW()
);

-- ÉTAPE 3: Migrer les spots existants vers les nouveaux types
-- Migration automatique basée sur la logique métier

-- Migrer "Plongée bouteille" vers "Plongée bouteille NEW"
UPDATE spots 
SET type_id = (SELECT id FROM spot_types WHERE name = 'Plongée bouteille NEW' LIMIT 1)
WHERE type_id = (SELECT id FROM spot_types WHERE name = 'Plongée bouteille' LIMIT 1);

-- Migrer "Boutique" et "Magasin de plongée" vers "Boutiques NEW"  
UPDATE spots 
SET type_id = (SELECT id FROM spot_types WHERE name = 'Boutiques NEW' LIMIT 1)
WHERE type_id IN (
    SELECT id FROM spot_types WHERE name IN ('Boutique', 'Magasin de plongée')
);

-- Migrer "Snorkeling" vers "Randonnée sous-marine NEW"
UPDATE spots 
SET type_id = (SELECT id FROM spot_types WHERE name = 'Randonnée sous-marine NEW' LIMIT 1)
WHERE type_id = (SELECT id FROM spot_types WHERE name = 'Snorkeling' LIMIT 1);

-- Migrer les types tronqués (comme "Cl") vers un type par défaut
UPDATE spots 
SET type_id = (SELECT id FROM spot_types WHERE name = 'Plongée bouteille NEW' LIMIT 1)
WHERE type_id IN (
    SELECT id FROM spot_types 
    WHERE name IS NULL OR LENGTH(name) < 3 OR name = 'Cl'
);

-- ÉTAPE 4: Maintenant supprimer les anciens types corrompus
DELETE FROM spot_types WHERE name IN (
    'Plongée bouteille',
    'Boutique',
    'Magasin de plongée', 
    'Snorkeling',
    'Cl'
) OR name IS NULL OR LENGTH(name) < 3;

-- ÉTAPE 5: Renommer les nouveaux types (enlever "NEW")
UPDATE spot_types SET name = 'Plongée bouteille' WHERE name = 'Plongée bouteille NEW';
UPDATE spot_types SET name = 'Apnée' WHERE name = 'Apnée NEW';
UPDATE spot_types SET name = 'Randonnée sous-marine' WHERE name = 'Randonnée sous-marine NEW';
UPDATE spot_types SET name = 'Photo sous-marine' WHERE name = 'Photo sous-marine NEW';
UPDATE spot_types SET name = 'Clubs' WHERE name = 'Clubs NEW';
UPDATE spot_types SET name = 'Professionnels' WHERE name = 'Professionnels NEW';
UPDATE spot_types SET name = 'Bases fédérales' WHERE name = 'Bases fédérales NEW';
UPDATE spot_types SET name = 'Boutiques' WHERE name = 'Boutiques NEW';

-- ÉTAPE 6: Vérification finale - doit retourner 8 enregistrements
SELECT 
    name,
    category,
    color_code,
    is_active,
    (SELECT COUNT(*) FROM spots WHERE type_id = spot_types.id) as spots_using_this_type
FROM spot_types 
WHERE is_active = true
ORDER BY 
    CASE category 
        WHEN 'Activity' THEN 1
        WHEN 'Structure' THEN 2  
        WHEN 'Shop' THEN 3
    END,
    name;

-- ÉTAPE 7: Statistiques finales
SELECT 
    category,
    COUNT(*) as count
FROM spot_types 
WHERE is_active = true
GROUP BY category
ORDER BY category;

-- ÉTAPE 8: Vérification des références
SELECT 
    'Total spots with valid type references' as check_type,
    COUNT(*) as count
FROM spots s
JOIN spot_types st ON s.type_id = st.id
WHERE st.is_active = true;

SELECT 
    'Spots with invalid/missing type references' as check_type,
    COUNT(*) as count
FROM spots s
LEFT JOIN spot_types st ON s.type_id = st.id
WHERE st.id IS NULL OR st.is_active = false;
INSERT INTO spot_types (
    id, name, icon_path, color_code, requires_expert_validation, 
    validation_criteria, category, description, is_active, created_at, updated_at
) VALUES 
(
    gen_random_uuid(),
    'Plongée bouteille',
    'marker_scuba.png',
    '#0077BE',
    true,
    '{"RequiredFields": ["DifficultyLevel", "SafetyNotes"], "MaxDepthRange": [0, 100]}',
    'Activity',
    'Sites de plongée avec bouteille (tous niveaux - récréative et technique)',
    true,
    NOW(),
    NOW()
),
(
    gen_random_uuid(),
    'Apnée',
    'marker_freediving.png',
    '#4169E1',
    true,
    '{"RequiredFields": ["DifficultyLevel", "SafetyNotes"], "MaxDepthRange": [0, 50]}',
    'Activity',
    'Sites de plongée en apnée et freediving',
    true,
    NOW(),
    NOW()
),
(
    gen_random_uuid(),
    'Randonnée sous-marine',
    'marker_snorkeling.png',
    '#87CEEB',
    false,
    '{"RequiredFields": ["DifficultyLevel", "SafetyNotes"], "MaxDepthRange": [0, 5]}',
    'Activity',
    'Sites de randonnée palmée et snorkeling',
    true,
    NOW(),
    NOW()
),
(
    gen_random_uuid(),
    'Photo sous-marine',
    'marker_photography.png',
    '#5DADE2',
    false,
    '{"RequiredFields": ["DifficultyLevel"]}',
    'Activity',
    'Sites d''intérêt pour la photographie sous-marine',
    true,
    NOW(),
    NOW()
),

-- === STRUCTURES (variations de verts) ===
(
    gen_random_uuid(),
    'Clubs',
    'marker_club.png',
    '#228B22',
    false,
    '{"RequiredFields": ["Description"]}',
    'Structure',
    'Clubs de plongée et associations',
    true,
    NOW(),
    NOW()
),
(
    gen_random_uuid(),
    'Professionnels',
    'marker_professional.png',
    '#32CD32',
    false,
    '{"RequiredFields": ["Description", "ContactInfo"]}',
    'Structure',
    'Centres de plongée, instructeurs et guides professionnels',
    true,
    NOW(),
    NOW()
),
(
    gen_random_uuid(),
    'Bases fédérales',
    'marker_federal.png',
    '#90EE90',
    false,
    '{"RequiredFields": ["Description", "ContactInfo"]}',
    'Structure',
    'Bases et installations officielles des fédérations',
    true,
    NOW(),
    NOW()
),

-- === COMMERCES (orange) ===
(
    gen_random_uuid(),
    'Boutiques',
    'marker_shop.png',
    '#FFA500',
    false,
    '{"RequiredFields": ["Description", "ContactInfo"], "commercial": true}',
    'Shop',
    'Magasins et services commerciaux liés à la plongée',
    true,
    NOW(),
    NOW()
);

-- 3. Vérification - doit retourner 8 enregistrements
SELECT 
    name,
    category,
    color_code,
    is_active
FROM spot_types 
WHERE is_active = true
ORDER BY 
    CASE category 
        WHEN 'Activity' THEN 1
        WHEN 'Structure' THEN 2  
        WHEN 'Shop' THEN 3
    END,
    name;

-- 4. Statistiques finales
SELECT 
    category,
    COUNT(*) as count
FROM spot_types 
WHERE is_active = true
GROUP BY category
ORDER BY category;