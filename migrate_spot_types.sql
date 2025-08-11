-- Migration pour la nouvelle structure hiérarchique des types de spots
-- Basé sur MigrateToNewSpotTypeStructure.cs

-- 1. SAUVEGARDE TEMPORAIRE (créer une table temporaire pour garder les données des spots)
CREATE TABLE IF NOT EXISTS temp_spots_backup AS 
SELECT s.*, st.Name as OldTypeName 
FROM Spots s 
JOIN SpotTypes st ON s.TypeId = st.Id;

-- 2. DÉSACTIVER LES ANCIENS TYPES DE SPOTS
UPDATE SpotTypes 
SET IsActive = 0 
WHERE Name IN ('Plongée récréative', 'Plongée technique');

-- 3. SUPPRIMER LES NOUVEAUX TYPES S'ILS EXISTENT DÉJÀ (nettoyage)
DELETE FROM SpotTypes WHERE Name IN (
    'Plongée bouteille', 'Apnée', 'Randonnée sous-marine', 'Photo sous-marine',
    'Clubs', 'Professionnels', 'Bases fédérales', 'Boutiques'
);

-- 4. CRÉER LES NOUVEAUX TYPES DE SPOTS

-- === ACTIVITÉS (variations de bleus) ===
INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
VALUES 
('Plongée bouteille', 'marker_scuba.png', '#0077BE', 0, 'Sites de plongée avec bouteille (tous niveaux - récréative et technique)', 1, 
 '{"RequiredFields":["MaxDepth","DifficultyLevel","SafetyNotes"],"MaxDepthRange":[0,200]}', 1);

INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
VALUES 
('Apnée', 'marker_freediving.png', '#4A90E2', 1, 'Sites adaptés à la plongée en apnée', 1, 
 '{"RequiredFields":["MaxDepth","DifficultyLevel","SafetyNotes"],"MaxDepthRange":[0,30]}', 1);

INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
VALUES 
('Randonnée sous-marine', 'marker_snorkeling.png', '#87CEEB', 2, 'Sites de surface accessibles pour la randonnée sous-marine', 0, 
 '{"RequiredFields":["DifficultyLevel","SafetyNotes"],"MaxDepthRange":[0,5]}', 1);

INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
VALUES 
('Photo sous-marine', 'marker_photography.png', '#5DADE2', 3, 'Sites d''intérêt pour la photographie sous-marine', 0, 
 '{"RequiredFields":["DifficultyLevel"]}', 1);

-- === STRUCTURES (variations de verts) ===
INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
VALUES 
('Clubs', 'marker_club.png', '#228B22', 4, 'Clubs de plongée et associations', 0, 
 '{"RequiredFields":["Description"]}', 1);

INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
VALUES 
('Professionnels', 'marker_pro.png', '#32CD32', 4, 'Centres de plongée, instructeurs et guides professionnels', 1, 
 '{"RequiredFields":["Description","SafetyNotes"]}', 1);

INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
VALUES 
('Bases fédérales', 'marker_federal.png', '#90EE90', 4, 'Bases fédérales et structures officielles', 1, 
 '{"RequiredFields":["Description","SafetyNotes"]}', 1);

-- === BOUTIQUES (tons oranges) ===
INSERT INTO SpotTypes (Name, IconPath, ColorCode, Category, Description, RequiresExpertValidation, ValidationCriteria, IsActive) 
VALUES 
('Boutiques', 'marker_shop.png', '#FF8C00', 4, 'Magasins de matériel de plongée et équipements sous-marins', 0, 
 '{"RequiredFields":["Description"]}', 1);

-- 5. MIGRATION DES SPOTS EXISTANTS
-- Migrer tous les spots de plongée récréative et technique vers plongée bouteille
UPDATE Spots 
SET TypeId = (SELECT Id FROM SpotTypes WHERE Name = 'Plongée bouteille' AND IsActive = 1 LIMIT 1)
WHERE TypeId IN (
    SELECT Id FROM SpotTypes 
    WHERE Name IN ('Plongée récréative', 'Plongée technique') AND IsActive = 0
);

-- 6. VÉRIFICATIONS FINALES
-- Afficher le nombre de nouveaux types créés
SELECT 'Types de spots actifs:' as Info, COUNT(*) as Nombre FROM SpotTypes WHERE IsActive = 1;

-- Afficher tous les types actifs
SELECT Name, ColorCode, Category, IsActive FROM SpotTypes WHERE IsActive = 1 ORDER BY Category, Name;

-- Afficher les spots migrés
SELECT COUNT(*) as 'Spots migrés vers Plongée bouteille' 
FROM Spots s 
JOIN SpotTypes st ON s.TypeId = st.Id 
WHERE st.Name = 'Plongée bouteille';

-- 7. NETTOYAGE DE LA SAUVEGARDE TEMPORAIRE
DROP TABLE IF EXISTS temp_spots_backup;

-- Affichage final
SELECT 'Migration terminée!' as Status;