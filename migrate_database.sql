-- ==================================================
-- MIGRATION SCRIPT : Nouvelle organisation des types de spots
-- ==================================================
-- Ce script migre la base de données existante vers la nouvelle structure
-- avec 8 types de spots organisés en catégories hiérarchiques
-- ==================================================

USE subexplore_dev;

-- 1. SAUVEGARDE DES DONNÉES EXISTANTES
-- Créer une table temporaire pour sauvegarder les spots existants
CREATE TABLE IF NOT EXISTS temp_spots_backup AS SELECT * FROM Spots;

-- 2. DÉSACTIVER LES ANCIENS TYPES DE SPOTS
UPDATE SpotTypes 
SET IsActive = 0 
WHERE Name IN ('Plongée récréative', 'Plongée technique');

-- 3. CRÉER LES NOUVEAUX TYPES DE SPOTS
-- Supprimer les nouveaux types s'ils existent déjà (au cas où)
DELETE FROM SpotTypes WHERE Name IN (
    'Plongée bouteille', 'Apnée', 'Randonnée sous-marine', 'Photo sous-marine',
    'Clubs', 'Professionnels', 'Bases fédérales', 'Boutiques'
);

-- === ACTIVITÉS (variations de bleus) ===
INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
VALUES 
('Plongée bouteille', 'marker_scuba.png', '#0077BE', 0, 'Sites de plongée avec bouteille (tous niveaux - récréative et technique)', 1, 
 '{"RequiredFields":["MaxDepth","DifficultyLevel","SafetyNotes"],"MaxDepthRange":[0,200]}', 1),

('Apnée', 'marker_freediving.png', '#4A90E2', 1, 'Sites adaptés à la plongée en apnée', 1, 
 '{"RequiredFields":["MaxDepth","DifficultyLevel","SafetyNotes"],"MaxDepthRange":[0,30]}', 1),

('Randonnée sous-marine', 'marker_snorkeling.png', '#87CEEB', 2, 'Sites de surface accessibles pour la randonnée sous-marine', 0, 
 '{"RequiredFields":["DifficultyLevel","SafetyNotes"],"MaxDepthRange":[0,5]}', 1),

('Photo sous-marine', 'marker_photography.png', '#5DADE2', 3, 'Sites d''intérêt pour la photographie sous-marine', 0, 
 '{"RequiredFields":["DifficultyLevel"]}', 1),

-- === STRUCTURES (variations de verts) ===
('Clubs', 'marker_club.png', '#228B22', 4, 'Clubs de plongée et associations', 0, 
 '{"RequiredFields":["Description"]}', 1),

('Professionnels', 'marker_pro.png', '#32CD32', 4, 'Centres de plongée, instructeurs et guides professionnels', 1, 
 '{"RequiredFields":["Description","SafetyNotes"]}', 1),

('Bases fédérales', 'marker_federal.png', '#90EE90', 4, 'Bases fédérales et structures officielles', 1, 
 '{"RequiredFields":["Description","SafetyNotes"]}', 1),

-- === BOUTIQUES (tons oranges) ===
('Boutiques', 'marker_shop.png', '#FF8C00', 4, 'Magasins de matériel de plongée et équipements sous-marins', 0, 
 '{"RequiredFields":["Description"]}', 1);

-- 4. MIGRATION DES SPOTS EXISTANTS
-- Récupérer l'ID du nouveau type "Plongée bouteille"
SET @new_diving_type_id = (SELECT Id FROM SpotTypes WHERE Name = 'Plongée bouteille' AND IsActive = 1 LIMIT 1);

-- Migrer tous les spots de plongée récréative et technique vers plongée bouteille
UPDATE Spots 
SET TypeId = @new_diving_type_id
WHERE TypeId IN (
    SELECT Id FROM SpotTypes 
    WHERE Name IN ('Plongée récréative', 'Plongée technique') AND IsActive = 0
);

-- 5. VÉRIFICATION DES RÉSULTATS
SELECT 'RÉSULTATS DE LA MIGRATION:' as Status;

SELECT 
    'Types de spots actifs' as Category,
    COUNT(*) as Count
FROM SpotTypes 
WHERE IsActive = 1

UNION ALL

SELECT 
    'Types de spots inactifs' as Category,
    COUNT(*) as Count
FROM SpotTypes 
WHERE IsActive = 0

UNION ALL

SELECT 
    'Total des spots' as Category,
    COUNT(*) as Count
FROM Spots

UNION ALL

SELECT 
    'Spots migrés vers Plongée bouteille' as Category,
    COUNT(*) as Count
FROM Spots 
WHERE TypeId = @new_diving_type_id;

-- Afficher tous les types actifs avec leurs détails
SELECT 
    'DÉTAIL DES NOUVEAUX TYPES:' as Info,
    '' as Name, '' as ColorCode, '' as Category;

SELECT 
    Name,
    ColorCode,
    CASE Category
        WHEN 0 THEN 'Diving'
        WHEN 1 THEN 'Freediving'
        WHEN 2 THEN 'Snorkeling'
        WHEN 3 THEN 'UnderwaterPhotography'
        WHEN 4 THEN 'Other'
        ELSE CAST(Category AS CHAR)
    END as Category,
    IsActive
FROM SpotTypes 
WHERE IsActive = 1
ORDER BY Category, Name;

-- Afficher les catégories pour le filtrage hiérarchique
SELECT 'CATÉGORIES POUR LE FILTRAGE:' as Info;

SELECT 'Activités' as MainCategory, Name as SubType 
FROM SpotTypes 
WHERE IsActive = 1 AND Name IN ('Plongée bouteille', 'Apnée', 'Randonnée sous-marine', 'Photo sous-marine')

UNION ALL

SELECT 'Structures' as MainCategory, Name as SubType 
FROM SpotTypes 
WHERE IsActive = 1 AND Name IN ('Clubs', 'Professionnels', 'Bases fédérales')

UNION ALL

SELECT 'Boutiques' as MainCategory, Name as SubType 
FROM SpotTypes 
WHERE IsActive = 1 AND Name IN ('Boutiques')

ORDER BY MainCategory, SubType;

-- 6. NETTOYAGE (optionnel - décommente si tu veux supprimer la sauvegarde)
-- DROP TABLE IF EXISTS temp_spots_backup;

-- ==================================================
-- MIGRATION TERMINÉE !
-- ==================================================
-- Tu peux maintenant tester l'application avec les nouveaux types de spots
-- Les 8 types sont maintenant disponibles et organisés en catégories :
-- - ACTIVITÉS : Plongée bouteille, Apnée, Randonnée sous-marine, Photo sous-marine
-- - STRUCTURES : Clubs, Professionnels, Bases fédérales  
-- - BOUTIQUES : Boutiques
-- ==================================================