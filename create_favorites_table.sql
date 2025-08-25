-- ========================================
-- CREATION TABLE USER_FAVORITE_SPOTS SUPABASE
-- ========================================
-- Script SQL pour cr\u00e9er la table des favoris dans Supabase

-- Cr\u00e9ation de la table user_favorite_spots
CREATE TABLE IF NOT EXISTS user_favorite_spots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    spot_id UUID NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    notes TEXT,
    priority INTEGER DEFAULT 5 CHECK (priority >= 1 AND priority <= 10),
    notification_enabled BOOLEAN DEFAULT true,
    
    -- Contraintes
    UNIQUE(user_id, spot_id),
    
    -- Cl\u00e9s \u00e9trang\u00e8res (si les tables users et spots existent)
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (spot_id) REFERENCES spots(id) ON DELETE CASCADE
);

-- Index pour am\u00e9liorer les performances
CREATE INDEX IF NOT EXISTS idx_user_favorite_spots_user_id ON user_favorite_spots(user_id);
CREATE INDEX IF NOT EXISTS idx_user_favorite_spots_spot_id ON user_favorite_spots(spot_id);
CREATE INDEX IF NOT EXISTS idx_user_favorite_spots_priority ON user_favorite_spots(priority DESC);
CREATE INDEX IF NOT EXISTS idx_user_favorite_spots_created_at ON user_favorite_spots(created_at DESC);

-- Politique RLS (Row Level Security) pour s\u00e9curiser l'acc\u00e8s
ALTER TABLE user_favorite_spots ENABLE ROW LEVEL SECURITY;

-- Politique permettant aux utilisateurs de voir/modifier uniquement leurs propres favoris
CREATE POLICY IF NOT EXISTS "Users can manage their own favorites" ON user_favorite_spots
    FOR ALL USING (auth.uid() = user_id);

-- Politique permettant la lecture publique des statistiques (nombre de favoris par spot)
CREATE POLICY IF NOT EXISTS "Public can read favorite counts" ON user_favorite_spots
    FOR SELECT USING (true);

-- Fonction trigger pour mettre \u00e0 jour updated_at
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Trigger pour updated_at
DROP TRIGGER IF EXISTS update_user_favorite_spots_updated_at ON user_favorite_spots;
CREATE TRIGGER update_user_favorite_spots_updated_at
    BEFORE UPDATE ON user_favorite_spots
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Commentaires pour documentation
COMMENT ON TABLE user_favorite_spots IS 'Table des spots favoris des utilisateurs';
COMMENT ON COLUMN user_favorite_spots.id IS 'Identifiant unique du favori';
COMMENT ON COLUMN user_favorite_spots.user_id IS 'ID de l utilisateur propri\u00e9taire';
COMMENT ON COLUMN user_favorite_spots.spot_id IS 'ID du spot en favoris';
COMMENT ON COLUMN user_favorite_spots.priority IS 'Priorit\u00e9 du favori (1-10, 10 = plus important)';
COMMENT ON COLUMN user_favorite_spots.notification_enabled IS 'Notifications activ\u00e9es pour ce favori';
COMMENT ON COLUMN user_favorite_spots.notes IS 'Notes personnelles sur le spot';

-- Vue pour les statistiques des favoris par spot
CREATE OR REPLACE VIEW spot_favorites_stats AS
SELECT 
    spot_id,
    COUNT(*) as total_favorites,
    COUNT(CASE WHEN notification_enabled THEN 1 END) as notifications_enabled,
    AVG(priority::float) as average_priority,
    MIN(created_at) as first_favorited,
    MAX(created_at) as last_favorited
FROM user_favorite_spots 
GROUP BY spot_id;

COMMENT ON VIEW spot_favorites_stats IS 'Vue des statistiques de favoris par spot';

-- Vue pour les favoris d'un utilisateur avec d\u00e9tails
CREATE OR REPLACE VIEW user_favorites_with_details AS
SELECT 
    ufs.*,
    s.name as spot_name,
    s.latitude,
    s.longitude,
    s.difficulty_level,
    s.max_depth,
    st.name as spot_type_name
FROM user_favorite_spots ufs
LEFT JOIN spots s ON ufs.spot_id = s.id
LEFT JOIN spot_types st ON s.type_id = st.id;

COMMENT ON VIEW user_favorites_with_details IS 'Vue des favoris utilisateur avec d\u00e9tails du spot';

-- Acc\u00e8s aux vues avec RLS
ALTER VIEW spot_favorites_stats OWNER TO postgres;
ALTER VIEW user_favorites_with_details OWNER TO postgres;

-- Fonction pour obtenir les favoris d'un utilisateur
CREATE OR REPLACE FUNCTION get_user_favorites(p_user_id UUID, p_limit INTEGER DEFAULT 20, p_offset INTEGER DEFAULT 0)
RETURNS TABLE (
    id UUID,
    spot_id UUID,
    spot_name TEXT,
    priority INTEGER,
    notes TEXT,
    notification_enabled BOOLEAN,
    created_at TIMESTAMP WITH TIME ZONE,
    latitude DECIMAL,
    longitude DECIMAL,
    difficulty_level INTEGER
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        ufs.id,
        ufs.spot_id,
        s.name as spot_name,
        ufs.priority,
        ufs.notes,
        ufs.notification_enabled,
        ufs.created_at,
        s.latitude,
        s.longitude,
        s.difficulty_level
    FROM user_favorite_spots ufs
    JOIN spots s ON ufs.spot_id = s.id
    WHERE ufs.user_id = p_user_id
    ORDER BY ufs.priority DESC, ufs.created_at DESC
    LIMIT p_limit OFFSET p_offset;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

COMMENT ON FUNCTION get_user_favorites IS 'Fonction pour r\u00e9cup\u00e9rer les favoris d un utilisateur avec pagination';

-- Exemple d'insertion (pour test)
/*
INSERT INTO user_favorite_spots (user_id, spot_id, priority, notes) 
VALUES (
    '00000000-0000-0000-0000-000000000001'::UUID, -- ID utilisateur test
    '00000000-0000-0000-0000-000000000002'::UUID, -- ID spot test
    8, 
    'Excellent spot pour d\u00e9butants'
);
*/

-- V\u00e9rification finale
SELECT 
    table_name,
    is_insertable_into,
    table_type
FROM information_schema.tables 
WHERE table_name = 'user_favorite_spots';

COMMENT ON TABLE user_favorite_spots IS 'Table compl\u00e8te pour la gestion des favoris avec s\u00e9curit\u00e9 RLS et optimisations';