-- ========================================
-- NETTOYAGE FINAL - SUPPRESSION DES DOUBLONS SUPABASE
-- ========================================

-- ÉTAPE 1: Analyser les doublons actuels
SELECT 
    name,
    COUNT(*) as count,
    STRING_AGG(id::text, ', ') as ids
FROM spot_types 
WHERE is_active = true
GROUP BY name
ORDER BY name;

-- ÉTAPE 2: Identifier les types à garder (les plus récents)
WITH ranked_types AS (
    SELECT 
        *,
        ROW_NUMBER() OVER (PARTITION BY name ORDER BY created_at DESC) as rn
    FROM spot_types 
    WHERE is_active = true
)
SELECT 
    name,
    id,
    created_at,
    'KEEP' as action
FROM ranked_types 
WHERE rn = 1
UNION ALL
SELECT 
    name,
    id,
    created_at,
    'DELETE' as action
FROM ranked_types 
WHERE rn > 1
ORDER BY name, created_at DESC;

-- ÉTAPE 3: Migrer les spots vers les types à conserver
-- Pour chaque doublon, migrer vers la version la plus récente

-- Migrer "Plongée bouteille" vers la version la plus récente
UPDATE spots 
SET type_id = (
    SELECT id FROM spot_types 
    WHERE name = 'Plongée bouteille' AND is_active = true
    ORDER BY created_at DESC 
    LIMIT 1
)
WHERE type_id IN (
    SELECT id FROM spot_types 
    WHERE name = 'Plongée bouteille' AND is_active = true
    AND id != (
        SELECT id FROM spot_types 
        WHERE name = 'Plongée bouteille' AND is_active = true
        ORDER BY created_at DESC 
        LIMIT 1
    )
);

-- Répéter pour tous les types potentiels
UPDATE spots 
SET type_id = (
    SELECT id FROM spot_types 
    WHERE name = 'Apnée' AND is_active = true
    ORDER BY created_at DESC 
    LIMIT 1
)
WHERE type_id IN (
    SELECT id FROM spot_types 
    WHERE name = 'Apnée' AND is_active = true
    AND id != (
        SELECT id FROM spot_types 
        WHERE name = 'Apnée' AND is_active = true
        ORDER BY created_at DESC 
        LIMIT 1
    )
);

UPDATE spots 
SET type_id = (
    SELECT id FROM spot_types 
    WHERE name = 'Randonnée sous-marine' AND is_active = true
    ORDER BY created_at DESC 
    LIMIT 1
)
WHERE type_id IN (
    SELECT id FROM spot_types 
    WHERE name = 'Randonnée sous-marine' AND is_active = true
    AND id != (
        SELECT id FROM spot_types 
        WHERE name = 'Randonnée sous-marine' AND is_active = true
        ORDER BY created_at DESC 
        LIMIT 1
    )
);

UPDATE spots 
SET type_id = (
    SELECT id FROM spot_types 
    WHERE name = 'Photo sous-marine' AND is_active = true
    ORDER BY created_at DESC 
    LIMIT 1
)
WHERE type_id IN (
    SELECT id FROM spot_types 
    WHERE name = 'Photo sous-marine' AND is_active = true
    AND id != (
        SELECT id FROM spot_types 
        WHERE name = 'Photo sous-marine' AND is_active = true
        ORDER BY created_at DESC 
        LIMIT 1
    )
);

UPDATE spots 
SET type_id = (
    SELECT id FROM spot_types 
    WHERE name = 'Clubs' AND is_active = true
    ORDER BY created_at DESC 
    LIMIT 1
)
WHERE type_id IN (
    SELECT id FROM spot_types 
    WHERE name = 'Clubs' AND is_active = true
    AND id != (
        SELECT id FROM spot_types 
        WHERE name = 'Clubs' AND is_active = true
        ORDER BY created_at DESC 
        LIMIT 1
    )
);

UPDATE spots 
SET type_id = (
    SELECT id FROM spot_types 
    WHERE name = 'Professionnels' AND is_active = true
    ORDER BY created_at DESC 
    LIMIT 1
)
WHERE type_id IN (
    SELECT id FROM spot_types 
    WHERE name = 'Professionnels' AND is_active = true
    AND id != (
        SELECT id FROM spot_types 
        WHERE name = 'Professionnels' AND is_active = true
        ORDER BY created_at DESC 
        LIMIT 1
    )
);

UPDATE spots 
SET type_id = (
    SELECT id FROM spot_types 
    WHERE name = 'Bases fédérales' AND is_active = true
    ORDER BY created_at DESC 
    LIMIT 1
)
WHERE type_id IN (
    SELECT id FROM spot_types 
    WHERE name = 'Bases fédérales' AND is_active = true
    AND id != (
        SELECT id FROM spot_types 
        WHERE name = 'Bases fédérales' AND is_active = true
        ORDER BY created_at DESC 
        LIMIT 1
    )
);

UPDATE spots 
SET type_id = (
    SELECT id FROM spot_types 
    WHERE name = 'Boutiques' AND is_active = true
    ORDER BY created_at DESC 
    LIMIT 1
)
WHERE type_id IN (
    SELECT id FROM spot_types 
    WHERE name = 'Boutiques' AND is_active = true
    AND id != (
        SELECT id FROM spot_types 
        WHERE name = 'Boutiques' AND is_active = true
        ORDER BY created_at DESC 
        LIMIT 1
    )
);

-- ÉTAPE 4: Supprimer les doublons (garder seulement la version la plus récente de chaque nom)
DELETE FROM spot_types 
WHERE id IN (
    SELECT id FROM (
        SELECT 
            id,
            ROW_NUMBER() OVER (PARTITION BY name ORDER BY created_at DESC) as rn
        FROM spot_types 
        WHERE is_active = true
    ) ranked
    WHERE rn > 1
);

-- ========================================
-- VÉRIFICATIONS FINALES
-- ========================================

-- ÉTAPE 5: Vérification finale - DOIT retourner exactement 8 types uniques
SELECT 
    name,
    category,
    color_code,
    created_at,
    (SELECT COUNT(*) FROM spots WHERE type_id = spot_types.id) as spots_count
FROM spot_types 
WHERE is_active = true
ORDER BY 
    CASE category 
        WHEN 'Activity' THEN 1
        WHEN 'Structure' THEN 2  
        WHEN 'Shop' THEN 3
    END,
    name;

-- ÉTAPE 6: Comptage total - DOIT être 8
SELECT 
    'Total types actifs' as description,
    COUNT(*) as count
FROM spot_types 
WHERE is_active = true;

-- ÉTAPE 7: Statistiques par catégorie
SELECT 
    category,
    COUNT(*) as count
FROM spot_types 
WHERE is_active = true
GROUP BY category
ORDER BY category;

-- ÉTAPE 8: Vérifier l'intégrité des références
SELECT 
    'Spots avec références valides' as check_type,
    COUNT(*) as count
FROM spots s
JOIN spot_types st ON s.type_id = st.id
WHERE st.is_active = true;

SELECT 
    'Spots avec références invalides' as check_type,
    COUNT(*) as count
FROM spots s
LEFT JOIN spot_types st ON s.type_id = st.id
WHERE st.id IS NULL OR st.is_active = false;