-- 🔧 Script de diagnostic Supabase
-- Vérification de la structure et des données actuelles

-- 1. Vérifier l'existence des tables
SELECT table_name, table_type
FROM information_schema.tables 
WHERE table_schema = 'public'
ORDER BY table_name;

-- 2. Vérifier les enums PostgreSQL existants
SELECT enumtypid, enumlabel 
FROM pg_enum 
JOIN pg_type ON pg_enum.enumtypid = pg_type.oid
WHERE pg_type.typname = 'activity_category'
ORDER BY enumsortorder;

-- 3. Vérifier les données dans spot_types si la table existe
-- (Remplacer par une requête conditionnelle)
SELECT category, count(*) 
FROM spot_types 
GROUP BY category;

-- 4. Voir la structure de la table spot_types
\d spot_types;

-- 5. Vérifier les valeurs uniques dans category
SELECT DISTINCT category 
FROM spot_types;