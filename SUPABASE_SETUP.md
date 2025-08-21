# Configuration Supabase pour SubExplore

## 📋 Résumé des corrections effectuées

✅ **App.xaml.cs** : Corrigé pour utiliser CompleteLoginPage au lieu de la page manuelle
✅ **appsettings.json** : Clés API Supabase configurées avec vos vraies credentials
✅ **MauiProgram.cs** : DevelopmentConfigurationService activé 
✅ **Compilation** : Application compile avec 0 erreurs

## 🎯 Action requise : Créer l'utilisateur admin dans Supabase

**PROBLÈME RESTANT** : L'utilisateur `admin@subexplore.com` n'existe pas dans votre projet Supabase.

**SOLUTION** : Suivez l'Étape 2B ci-dessous pour créer l'utilisateur.

## ✅ Solution : Configurer vos propres clés Supabase

### Étape 1 : Créer un projet Supabase (✅ FAIT)

Votre projet Supabase est déjà créé :
- **URL** : https://iguvwnyehojvxkyqzaoi.supabase.co
- **Clés API** : Configurées dans appsettings.json
- **Status** : ✅ Connexion API réussie

### Étape 2 : Configurer l'authentification Supabase 🔐

**PROBLÈME ACTUEL** : L'utilisateur `admin@subexplore.com` n'existe pas dans votre projet Supabase.

#### A. Activer l'authentification

1. Dans votre projet Supabase, allez dans **Authentication** → **Settings**
2. Vérifiez que **Enable email confirmations** est activé/désactivé selon vos besoins
3. Dans **Authentication** → **Providers**, activez **Email** provider
4. Configurez les paramètres email si nécessaire

#### B. Créer un utilisateur admin

**Option 1 : Via l'interface Supabase (Recommandé)**
1. Allez dans **Authentication** → **Users**
2. Cliquez sur **Add user**
3. Entrez :
   - **Email** : `admin@subexplore.com`
   - **Password** : `Admin123!`
   - **Auto Confirm User** : ✅ (pour éviter la confirmation email)
4. Cliquez sur **Create user**

**Option 2 : Via SQL**
```sql
-- Insérer un utilisateur dans auth.users
INSERT INTO auth.users (
    instance_id,
    id,
    aud,
    role,
    email,
    encrypted_password,
    email_confirmed_at,
    created_at,
    updated_at,
    raw_app_meta_data,
    raw_user_meta_data,
    is_super_admin,
    confirmation_token,
    email_change,
    email_change_token_new,
    recovery_token
) VALUES (
    '00000000-0000-0000-0000-000000000000',
    gen_random_uuid(),
    'authenticated',
    'authenticated',
    'admin@subexplore.com',
    crypt('Admin123!', gen_salt('bf')),
    NOW(),
    NOW(),
    NOW(),
    '{"provider":"email","providers":["email"]}',
    '{}',
    FALSE,
    '',
    '',
    '',
    ''
);
```

### Étape 3 : Configuration Application (✅ FAIT)

✅ **appsettings.json** : Clés API configurées
✅ **MauiProgram.cs** : DevelopmentConfigurationService activé
✅ **App.xaml.cs** : CompleteLoginPage correctement utilisée

### Étape 4 : Test de connexion

Une fois l'utilisateur créé dans Supabase :

1. **Compilez l'application** : `dotnet build` ✅ 
2. **Lancez l'application sur émulateur Android**
3. **Utilisez les identifiants** :
   - Email : `admin@subexplore.com`
   - Mot de passe : `Admin123!`
4. **Vérifiez la connexion**

#### Logs attendus après création de l'utilisateur :
```
[SimpleAuthenticationService] ✅ Login successful for user: admin@subexplore.com
[App.xaml.cs] ✅ Supabase API login successful - Switching to AppShell
```

### Étape 5 : Configurer le schéma de base de données (Optionnel)

**Note** : Cette étape n'est nécessaire que si vous voulez utiliser des tables personnalisées. Supabase Auth fonctionne déjà avec ses tables intégrées.

1. Dans Supabase, allez dans **SQL Editor**
2. Exécutez le script SQL pour créer les tables nécessaires :

```sql
-- Créer les tables pour SubExplore
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email TEXT UNIQUE NOT NULL,
    username TEXT UNIQUE,
    first_name TEXT,
    last_name TEXT,
    password_hash TEXT NOT NULL,
    is_email_confirmed BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE spots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL,
    description TEXT,
    latitude DECIMAL(10, 8) NOT NULL,
    longitude DECIMAL(11, 8) NOT NULL,
    depth DECIMAL(5, 2),
    created_by UUID REFERENCES users(id),
    is_approved BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Activer RLS (Row Level Security)
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE spots ENABLE ROW LEVEL SECURITY;

-- Politiques de sécurité basiques
CREATE POLICY "Users can view own profile" ON users FOR SELECT USING (auth.uid() = id);
CREATE POLICY "Users can update own profile" ON users FOR UPDATE USING (auth.uid() = id);
CREATE POLICY "Everyone can view approved spots" ON spots FOR SELECT USING (is_approved = true);
CREATE POLICY "Users can create spots" ON spots FOR INSERT WITH CHECK (auth.uid() = created_by);
```

## 🧪 Mode Test Offline

L'application est actuellement configurée en mode test offline avec `OfflineTestConfigurationService`. 
Ce mode permet de :
- ✅ Tester l'interface utilisateur
- ✅ Naviguer dans l'application  
- ❌ Pas de connexion/inscription réelle
- ❌ Pas de synchronisation des données

## 🔧 Dépannage

### Erreur "Invalid API key"
- Vérifiez que vos clés API sont correctes
- Assurez-vous que le projet Supabase est actif
- Vérifiez que l'URL du projet est correcte

### Erreur de connexion réseau
- Vérifiez votre connexion internet
- Assurez-vous que Supabase n'est pas bloqué par un pare-feu

### Erreurs d'authentification
- Vérifiez que l'authentification est activée dans Supabase
- Configurez les providers d'authentification si nécessaire

## 📞 Support

Pour des questions spécifiques à SubExplore :
1. Vérifiez d'abord ce guide
2. Consultez la documentation Supabase : [https://supabase.com/docs](https://supabase.com/docs)
3. Vérifiez les logs de l'application pour des erreurs spécifiques