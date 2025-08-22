-- Script simple pour ajouter une boutique test dans Supabase
-- À exécuter directement dans l'éditeur SQL de Supabase

-- 1. Insérer d'abord le type de spot "Boutique" s'il n'existe pas
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM spot_types WHERE name = 'Boutique') THEN
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
        ) VALUES (
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
        );
    END IF;
END $$;

-- 2. Insérer une boutique d'exemple
DO $$
DECLARE
    boutique_type_id UUID;
BEGIN
    -- Récupérer l'ID du type Boutique
    SELECT id INTO boutique_type_id FROM spot_types WHERE name = 'Boutique' LIMIT 1;
    
    -- Insérer la boutique si elle n'existe pas déjà
    IF NOT EXISTS (SELECT 1 FROM spots WHERE name = 'AquaTech Diving Store') THEN
        INSERT INTO spots (
            id,
            name,
            description,
            latitude,
            longitude,
            depth,
            difficulty_level,
            current_strength,
            visibility,
            water_temperature,
            best_time_to_visit,
            access_instructions,
            safety_notes,
            required_equipment,
            additional_info,
            status,
            created_by,
            spot_type_id,
            is_validated,
            validation_score,
            created_at,
            updated_at
        ) VALUES (
            gen_random_uuid(),
            'AquaTech Diving Store',
            'Magasin spécialisé dans la vente et location d''équipements de plongée. Large gamme de détendeurs, combinaisons, masques et accessoires. Service de maintenance et réparation disponible.',
            43.2965,
            5.3698,
            NULL,  -- Pas de profondeur pour une boutique
            NULL,  -- Pas de niveau de difficulté
            NULL,  -- Pas de force du courant
            NULL,  -- Pas de visibilité
            NULL,  -- Pas de température de l'eau
            'Toute l''année',
            'Situé en centre-ville, proche du port. Parking disponible.',
            NULL,  -- Pas de notes de sécurité
            NULL,  -- Pas d'équipement requis
            '{"horaires": "Lun-Sam 9h-18h", "services": ["vente", "location", "réparation"], "specialites": ["détendeurs", "combinaisons", "accessoires"], "contact": "04 91 XX XX XX"}',
            'validated',
            gen_random_uuid(), -- created_by (utiliser un UUID existant d'utilisateur)
            boutique_type_id,
            true,
            100,
            NOW(),
            NOW()
        );
    END IF;
END $$;

-- Vérifier que l'insertion a fonctionné
SELECT 
    s.name,
    s.description,
    st.name as type_name,
    st.category,
    s.additional_info
FROM spots s
JOIN spot_types st ON s.spot_type_id = st.id
WHERE st.name = 'Boutique';