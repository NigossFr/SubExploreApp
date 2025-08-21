# Structure de la Base de Données SubExplore v1

## Vue d'ensemble

SubExplore utilise une base de données MySQL avec Entity Framework Core et le provider Pomelo.EntityFrameworkCore.MySql. La base de données est conçue pour supporter une application de communauté de sports sous-marins avec gestion des utilisateurs, des spots, des médias et des préférences.

## Tables Principales

### 1. Users (Utilisateurs)

Table centrale contenant les informations des utilisateurs de l'application.

| Colonne | Type | Contraintes | Description |
|---------|------|-------------|-------------|
| `Id` | int | PK, AUTO_INCREMENT | Identifiant unique |
| `Email` | varchar | REQUIRED, UNIQUE, EmailAddress | Email de l'utilisateur |
| `PasswordHash` | varchar | REQUIRED | Hash du mot de passe (BCrypt) |
| `Username` | varchar(30) | NULLABLE, UNIQUE, MinLength(3), Regex(^[a-zA-Z0-9_-]+$) | Nom d'utilisateur |
| `FirstName` | varchar(50) | REQUIRED | Prénom |
| `LastName` | varchar(50) | REQUIRED | Nom de famille |
| `AvatarUrl` | varchar | NULLABLE, URL | URL de l'avatar |
| `AccountType` | enum | REQUIRED, DEFAULT(Standard) | Type de compte |
| `SubscriptionStatus` | enum | REQUIRED, DEFAULT(Free) | Statut d'abonnement |
| `ExpertiseLevel` | enum | NULLABLE | Niveau d'expertise |
| `Certifications` | json | NULLABLE | Certifications de l'utilisateur |
| `CreatedAt` | datetime | REQUIRED, DEFAULT(NOW) | Date de création |
| `UpdatedAt` | datetime | NULLABLE | Date de mise à jour |
| `LastLogin` | datetime | NULLABLE | Dernière connexion |
| `IsEmailConfirmed` | boolean | DEFAULT(false) | Email confirmé |
| `ModeratorSpecialization` | enum | DEFAULT(None) | Spécialisation modérateur |
| `ModeratorStatus` | enum | DEFAULT(None) | Statut modérateur |
| `Permissions` | enum | DEFAULT(CreateSpots) | Permissions utilisateur |
| `ModeratorSince` | datetime | NULLABLE | Date nomination modérateur |
| `OrganizationId` | int | NULLABLE | ID organisation |

**Index :**
- `IX_Users_Email_Unique` (Email) - UNIQUE
- `IX_Users_Username_Unique` (Username) - UNIQUE
- `IX_Users_AccountType_Subscription` (AccountType, SubscriptionStatus)
- `IX_Users_CreatedAt_AccountType` (CreatedAt, AccountType)
- `IX_Users_ExpertiseLevel` (ExpertiseLevel)

**Relations :**
- One-to-One avec `UserPreferences`
- One-to-Many avec `Spots` (créés)
- One-to-Many avec `UserFavoriteSpots`

### 2. UserPreferences (Préférences Utilisateur)

Configuration et préférences personnalisées des utilisateurs.

| Colonne | Type | Contraintes | Description |
|---------|------|-------------|-------------|
| `Id` | int | PK, AUTO_INCREMENT | Identifiant unique |
| `UserId` | int | FK, REQUIRED, UNIQUE | Référence vers Users |
| `Theme` | varchar | DEFAULT('light') | Thème de l'interface |
| `DisplayNamePreference` | varchar | DEFAULT('username') | Préférence d'affichage du nom |
| `NotificationSettings` | json | DEFAULT('{}') | Paramètres de notifications |
| `Language` | varchar | DEFAULT('fr') | Langue de l'interface |
| `CreatedAt` | datetime | REQUIRED, DEFAULT(NOW) | Date de création |
| `UpdatedAt` | datetime | NULLABLE | Date de mise à jour |

**Index :**
- `IX_UserPreferences_UserId_Unique` (UserId) - UNIQUE
- `IX_UserPreferences_Language` (Language)
- `IX_UserPreferences_Theme` (Theme)

### 3. Spots (Sites de Plongée)

Table principale des sites de sports sous-marins.

| Colonne | Type | Contraintes | Description |
|---------|------|-------------|-------------|
| `Id` | int | PK, AUTO_INCREMENT | Identifiant unique |
| `CreatorId` | int | FK, REQUIRED | Référence vers Users |
| `Name` | varchar(100) | REQUIRED | Nom du spot |
| `Description` | text | REQUIRED | Description détaillée |
| `Latitude` | decimal(10,8) | REQUIRED, Range(-90,90) | Latitude GPS |
| `Longitude` | decimal(11,8) | REQUIRED, Range(-180,180) | Longitude GPS |
| `DifficultyLevel` | enum | REQUIRED | Niveau de difficulté |
| `TypeId` | int | FK, REQUIRED | Référence vers SpotTypes |
| `RequiredEquipment` | text | REQUIRED | Équipement requis |
| `SafetyNotes` | text | REQUIRED | Notes de sécurité |
| `BestConditions` | text | REQUIRED | Meilleures conditions |
| `CreatedAt` | datetime | REQUIRED, DEFAULT(NOW) | Date de création |
| `ValidationStatus` | enum | REQUIRED, DEFAULT(Pending) | Statut de validation |
| `LastSafetyReview` | datetime | NULLABLE | Dernière révision sécurité |
| `SafetyFlags` | json | NULLABLE | Drapeaux de sécurité |
| `MaxDepth` | int | NULLABLE, Range(0,200) | Profondeur maximale (m) |
| `CurrentStrength` | enum | NULLABLE | Force du courant |
| `HasMooring` | boolean | NULLABLE | Présence d'un mouillage |
| `BottomType` | varchar(100) | NULLABLE | Type de fond |

**Index :**
- `IX_Spots_Location_Geospatial` (Latitude, Longitude) - Géospatial
- `IX_Spots_TypeId` (TypeId)
- `IX_Spots_ValidationStatus` (ValidationStatus)
- `IX_Spots_CreatorId` (CreatorId)
- `IX_Spots_CreatedAt` (CreatedAt)
- `IX_Spots_DifficultyLevel` (DifficultyLevel)
- `IX_Spots_ValidatedByType_Location` (ValidationStatus, TypeId, Latitude, Longitude)
- `IX_Spots_Creator_Status_Date` (CreatorId, ValidationStatus, CreatedAt)
- `IX_Spots_Name_Search` (Name)
- `IX_Spots_Depth_Difficulty` (MaxDepth, DifficultyLevel)

**Relations :**
- Many-to-One avec `Users` (Creator)
- Many-to-One avec `SpotTypes` (Type)
- One-to-Many avec `SpotMedia`
- One-to-Many avec `UserFavoriteSpots`

### 4. SpotTypes (Types de Spots)

Catégories et types d'activités sous-marines.

| Colonne | Type | Contraintes | Description |
|---------|------|-------------|-------------|
| `Id` | int | PK, AUTO_INCREMENT | Identifiant unique |
| `Name` | varchar(50) | REQUIRED | Nom du type |
| `IconPath` | varchar(200) | NULLABLE | Chemin vers l'icône |
| `ColorCode` | varchar(7) | NULLABLE, Regex(^#([A-Fa-f0-9]{6}\|[A-Fa-f0-9]{3})$) | Code couleur hexadécimal |
| `RequiresExpertValidation` | boolean | REQUIRED | Validation experte requise |
| `ValidationCriteria` | json | NULLABLE | Critères de validation |
| `Category` | enum | REQUIRED | Catégorie d'activité |
| `Description` | text | NULLABLE | Description du type |
| `IsActive` | boolean | REQUIRED, DEFAULT(true) | Statut actif |
| `CreatedAt` | datetime | REQUIRED, DEFAULT(NOW) | Date de création |
| `UpdatedAt` | datetime | NULLABLE | Date de mise à jour |

**Index :**
- `IX_SpotTypes_IsActive` (IsActive)
- `IX_SpotTypes_Category` (Category)
- `IX_SpotTypes_Active_Category` (IsActive, Category)
- `IX_SpotTypes_RequiresValidation` (RequiresExpertValidation)

**Relations :**
- One-to-Many avec `Spots`

### 5. SpotMedia (Médias des Spots)

Fichiers multimédias associés aux spots.

| Colonne | Type | Contraintes | Description |
|---------|------|-------------|-------------|
| `Id` | int | PK, AUTO_INCREMENT | Identifiant unique |
| `SpotId` | int | FK, REQUIRED | Référence vers Spots |
| `MediaType` | enum | REQUIRED | Type de média |
| `MediaUrl` | varchar(500) | REQUIRED, URL | URL du fichier |
| `CreatedAt` | datetime | REQUIRED, DEFAULT(NOW) | Date de création |
| `Status` | enum | REQUIRED, DEFAULT(Pending) | Statut du média |
| `Caption` | text | NULLABLE | Légende |
| `IsPrimary` | boolean | REQUIRED | Image principale |
| `Width` | int | NULLABLE | Largeur en pixels |
| `Height` | int | NULLABLE | Hauteur en pixels |
| `FileSize` | bigint | NULLABLE, Range(0,5242880) | Taille en bytes (max 5MB) |
| `ContentType` | varchar | NULLABLE | Type MIME |

**Index :**
- `IX_SpotMedia_SpotId` (SpotId)
- `IX_SpotMedia_Spot_Type` (SpotId, MediaType)
- `IX_SpotMedia_CreatedAt` (CreatedAt)

**Relations :**
- Many-to-One avec `Spots`

### 6. UserFavoriteSpots (Spots Favoris)

Table de liaison pour les spots favoris des utilisateurs.

| Colonne | Type | Contraintes | Description |
|---------|------|-------------|-------------|
| `Id` | int | PK, AUTO_INCREMENT | Identifiant unique |
| `UserId` | int | FK, REQUIRED, Range(1,∞) | Référence vers Users |
| `SpotId` | int | FK, REQUIRED, Range(1,∞) | Référence vers Spots |
| `CreatedAt` | datetime | REQUIRED, DEFAULT(NOW) | Date d'ajout aux favoris |
| `UpdatedAt` | datetime | NULLABLE | Date de mise à jour |
| `Notes` | varchar(500) | NULLABLE | Notes personnelles |
| `Priority` | int | Range(1,10), DEFAULT(5) | Priorité (1=haute, 10=basse) |
| `NotificationEnabled` | boolean | DEFAULT(true) | Notifications activées |

**Index :**
- `IX_UserFavoriteSpots_User_Spot_Unique` (UserId, SpotId) - UNIQUE
- `IX_UserFavoriteSpots_UserId` (UserId)
- `IX_UserFavoriteSpots_SpotId` (SpotId)
- `IX_UserFavoriteSpots_User_Date` (UserId, CreatedAt)
- `IX_UserFavoriteSpots_User_Priority_Date` (UserId, Priority, CreatedAt)
- `IX_UserFavoriteSpots_User_Notifications` (UserId, NotificationEnabled)

**Relations :**
- Many-to-One avec `Users`
- Many-to-One avec `Spots`

### 7. EmailVerificationTokens (Tokens de Vérification Email)

Gestion des tokens de vérification d'adresse email.

| Colonne | Type | Contraintes | Description |
|---------|------|-------------|-------------|
| `Id` | int | PK, AUTO_INCREMENT | Identifiant unique |
| `UserId` | int | FK, REQUIRED | Référence vers Users |
| `TokenHash` | varchar(500) | REQUIRED | Hash du token de vérification |
| `Email` | varchar(255) | REQUIRED, EmailAddress | Email à vérifier |
| `ExpiresAt` | datetime | REQUIRED | Date d'expiration |
| `IsUsed` | boolean | DEFAULT(false) | Token utilisé |
| `UsedAt` | datetime | NULLABLE | Date d'utilisation |
| `CreatedFromIP` | varchar(45) | NULLABLE | IP de création |
| `UsedFromIP` | varchar(45) | NULLABLE | IP d'utilisation |
| `CreatedAt` | datetime | REQUIRED, DEFAULT(NOW) | Date de création |
| `AttemptCount` | int | DEFAULT(0) | Nombre de tentatives |
| `MaxAttempts` | int | DEFAULT(5) | Tentatives maximales |

**Index :**
- `IX_EmailVerificationTokens_TokenHash_Unique` (TokenHash) - UNIQUE
- `IX_EmailVerificationTokens_UserId` (UserId)
- `IX_EmailVerificationTokens_Email` (Email)
- `IX_EmailVerificationTokens_ExpiresAt` (ExpiresAt)
- `IX_EmailVerificationTokens_CreatedAt` (CreatedAt)
- `IX_EmailVerificationTokens_User_Date` (UserId, CreatedAt)
- `IX_EmailVerificationTokens_Used_Expires` (IsUsed, ExpiresAt)

**Relations :**
- Many-to-One avec `Users` (OnDelete: Cascade)

### 8. PasswordResetTokens (Tokens de Réinitialisation)

Gestion des tokens de réinitialisation de mot de passe.

| Colonne | Type | Contraintes | Description |
|---------|------|-------------|-------------|
| `Id` | int | PK, AUTO_INCREMENT | Identifiant unique |
| `UserId` | int | FK, REQUIRED | Référence vers Users |
| `TokenHash` | varchar(500) | REQUIRED | Hash du token de reset |
| `Email` | varchar(255) | REQUIRED, EmailAddress | Email du reset |
| `ExpiresAt` | datetime | REQUIRED | Date d'expiration |
| `IsUsed` | boolean | DEFAULT(false) | Token utilisé |
| `UsedAt` | datetime | NULLABLE | Date d'utilisation |
| `CreatedFromIP` | varchar(45) | NULLABLE | IP de création |
| `UsedFromIP` | varchar(45) | NULLABLE | IP d'utilisation |
| `CreatedAt` | datetime | REQUIRED, DEFAULT(NOW) | Date de création |
| `AttemptCount` | int | DEFAULT(0) | Nombre de tentatives |
| `MaxAttempts` | int | DEFAULT(3) | Tentatives maximales |
| `ResetReason` | varchar(200) | NULLABLE | Raison du reset |

**Index :**
- `IX_PasswordResetTokens_TokenHash_Unique` (TokenHash) - UNIQUE
- `IX_PasswordResetTokens_UserId` (UserId)
- `IX_PasswordResetTokens_Email` (Email)
- `IX_PasswordResetTokens_ExpiresAt` (ExpiresAt)
- `IX_PasswordResetTokens_CreatedAt` (CreatedAt)
- `IX_PasswordResetTokens_User_Date` (UserId, CreatedAt)
- `IX_PasswordResetTokens_Email_Date` (Email, CreatedAt)
- `IX_PasswordResetTokens_Used_Expires` (IsUsed, ExpiresAt)

**Relations :**
- Many-to-One avec `Users` (OnDelete: Cascade)

### 9. RevokedTokens (Tokens Révoqués)

Gestion des tokens JWT révoqués pour la sécurité.

| Colonne | Type | Contraintes | Description |
|---------|------|-------------|-------------|
| `Id` | int | PK, AUTO_INCREMENT | Identifiant unique |
| `TokenHash` | varchar(500) | REQUIRED, UNIQUE | Hash du token révoqué |
| `TokenType` | varchar(50) | REQUIRED | Type de token |
| `UserId` | int | FK, NULLABLE | Référence vers Users |
| `RevokedAt` | datetime | REQUIRED, DEFAULT(NOW) | Date de révocation |
| `ExpiresAt` | datetime | NULLABLE | Date d'expiration |
| `RevocationReason` | varchar(200) | NULLABLE | Raison de la révocation |
| `RevocationIpAddress` | varchar(45) | NULLABLE | Adresse IP de révocation |

**Index :**
- `IX_RevokedTokens_TokenHash` (TokenHash) - UNIQUE
- `IX_RevokedTokens_UserId` (UserId)  
- `IX_RevokedTokens_RevokedAt` (RevokedAt)
- `IX_RevokedTokens_ExpiresAt` (ExpiresAt)

**Relations :**
- Many-to-One avec `Users` (OnDelete: SetNull)

## Énumérations Supplémentaires

### ModeratorSpecialization (Spécialisation Modérateur)
- `None` (0) : Aucune spécialisation
- `RecreationalDiving` (1) : Plongée récréative
- `TechnicalDiving` (2) : Plongée technique
- `Freediving` (3) : Apnée
- `SnorkelingHiking` (4) : Randonnée aquatique
- `UnderwaterPhotography` (5) : Photographie sous-marine
- `DiveSpots` (6) : Sites de plongée
- `FreediveSpots` (7) : Sites d'apnée
- `SnorkelSpots` (8) : Sites de randonnée
- `SafetyAndRegulations` (9) : Sécurité et réglementation
- `MarineConservation` (10) : Conservation marine
- `CommunityManagement` (11) : Gestion communautaire

### ModeratorStatus (Statut Modérateur)
- `None` (0) : N'est pas modérateur
- `Probationary` (1) : Période probatoire
- `Active` (2) : Actif
- `Suspended` (3) : Suspendu
- `Retired` (4) : Retraité

### UserPermissions (Permissions Utilisateur)
Système de flags binaires pour contrôle granulaire :
- `None` (0) : Aucune permission
- `CreateSpots` (1) : Créer des spots
- `ValidateSpots` (2) : Valider les spots
- `ModerateContent` (4) : Modérer le contenu
- `ManageOrganization` (8) : Gérer l'organisation
- `ProfessionalFeatures` (16) : Fonctionnalités professionnelles
- `NominateModerators` (32) : Nommer des modérateurs
- `AdminAccess` (64) : Accès administration
- `ManageUsers` (128) : Gérer les utilisateurs
- `ViewModerationLogs` (256) : Voir les logs de modération
- `ViewAnalytics` (512) : Accès aux analytiques

## Énumérations

### AccountType (Type de Compte)
- `Standard` (0) : Utilisateur standard
- `ExpertModerator` (1) : Modérateur expert
- `VerifiedProfessional` (2) : Professionnel vérifié
- `Administrator` (3) : Administrateur

### SubscriptionStatus (Statut d'Abonnement)
- `Free` : Gratuit
- `Premium` : Premium
- `PremiumPlus` : Premium Plus
- `Suspended` : Suspendu

### ExpertiseLevel (Niveau d'Expertise)
- `Beginner` : Débutant
- `Intermediate` : Intermédiaire
- `Advanced` : Avancé
- `Expert` : Expert
- `Professional` : Professionnel

### DifficultyLevel (Niveau de Difficulté)
- `Beginner` (1) : Débutant
- `Intermediate` (2) : Intermédiaire
- `Advanced` (3) : Avancé
- `Expert` (4) : Expert
- `TechnicalOnly` (5) : Technique uniquement

### SpotValidationStatus (Statut de Validation)
- `Draft` : Brouillon
- `Pending` : En attente
- `UnderReview` : En cours de révision
- `NeedsRevision` : Nécessite révision
- `SafetyReview` : Révision sécurité
- `Approved` : Approuvé
- `Rejected` : Rejeté
- `Archived` : Archivé

### CurrentStrength (Force du Courant)
- `None` : Aucun
- `Weak` : Faible
- `Moderate` : Modéré
- `Strong` : Fort
- `Extreme` : Extrême

### ActivityCategory (Catégorie d'Activité)
- `Activity` : Toutes les activités sous-marines
- `Structure` : Clubs, centres, bases fédérales
- `Shop` : Boutiques et magasins
- `Other` : Autres types

**Notes** : Les anciennes valeurs `Diving`, `Freediving`, `Snorkeling`, `UnderwaterPhotography` sont obsolètes et remplacées par `Activity`.

### MediaType (Type de Média)
- `Photo` : Photo
- `Video` : Vidéo
- `Panorama` : Panorama
- `Document` : Document

### MediaStatus (Statut de Média)
- `Pending` : En attente
- `Processing` : En traitement
- `Active` : Actif
- `Rejected` : Rejeté
- `Archived` : Archivé
- `Failed` : Échec

## Données de Test (Seed Data)

### Utilisateur Administrateur
- **Email :** admin@subexplore.com
- **Username :** admin
- **Type :** Administrator
- **Abonnement :** Premium
- **Expertise :** Professional
- **EmailConfirmé :** true
- **Permissions :** Toutes les permissions (1023)
- **Mot de passe par défaut :** Admin123!

### Types de Spots Préconfigurés
1. **Apnée** - Bleu (#00B4D8) - Catégorie: Activity - Validation experte requise
2. **Photo sous-marine** - Turquoise (#2EC4B6) - Catégorie: Activity - Validation simple
3. **Plongée récréative** - Bleu foncé (#006994) - Catégorie: Activity - Validation experte requise
4. **Plongée technique** - Orange (#FF9F1C) - Catégorie: Activity - Validation experte requise
5. **Randonnée sous marine** - Bleu clair (#48CAE4) - Catégorie: Activity - Validation simple

### Spots d'Exemple
1. **Calanque de Sormiou** - Plongée récréative (25m, Intermédiaire)
2. **Île Maïre** - Plongée technique (40m, Avancé)
3. **Baie de Cassis** - Apnée (15m, Débutant)
4. **Port-Cros** - Photo sous-marine (12m, Débutant)
5. **Sentier Sous-Marin de Banyuls** - Randonnée (5m, Débutant)

## Configuration Technique

### Base de Données
- **Moteur :** MySQL 8.0+
- **Provider :** Pomelo.EntityFrameworkCore.MySql 8.0.0
- **Charset :** utf8mb4
- **Collation :** utf8mb4_unicode_ci
- **Connection Pooling :** Activé (Min: 1, Max: 10)
- **Timeout :** 30 secondes

### Optimisations
- Index géospatiaux pour les recherches de proximité
- Index composites pour les requêtes fréquentes
- Contraintes d'intégrité référentielle
- Validation au niveau modèle et base de données
- Stratégies de cache intégrées

### Sécurité
- Hash des mots de passe avec BCrypt
- Gestion des tokens révoqués
- Tokens de vérification email sécurisés
- Tokens de réinitialisation mot de passe
- Contraintes de validation strictes
- Audit trail sur les actions sensibles
- Protection contre l'injection SQL via EF Core
- Limitation des tentatives (rate limiting)
- Traçabilité IP pour audit sécurisé

### Performance
- Index optimisés pour les requêtes géographiques
- Pagination automatique sur les listes
- Lazy loading configuré
- Connection pooling
- Requêtes SQL optimisées par EF Core

## Migration et Versioning

La base de données utilise Entity Framework Core Migrations pour le versioning et les mises à jour de schéma. Les migrations sont stockées dans le dossier `Migrations/` et permettent un déploiement incrémental des changements de structure.

### Migrations Actuelles
1. **AddIsEmailConfirmedMigration** : Ajout du champ `IsEmailConfirmed`
2. **AddPerformanceIndexesMigration** : Indexes de performance
3. **AddUserFavoriteSpotMigration** : Système de favoris
4. **AddUserRoleHierarchyMigration** : Hiérarchie des rôles utilisateur
5. **FixSpotTypeCategoryMapping** : Correction mapping catégories
6. **FixSpotTypeDataConsistency** : Cohérence des données
7. **MigrateToNewSpotTypeStructure** : Nouvelle structure SpotType
8. **UpdateActivityCategoryStructure** : Mise à jour catégories activités

### Commandes Utiles
```bash
# Ajouter une migration
dotnet ef migrations add NomDeLaMigration

# Mettre à jour la base de données
dotnet ef database update

# Supprimer la dernière migration
dotnet ef migrations remove

# Générer un script SQL pour la production
dotnet ef migrations script --environment Production

# Appliquer les migrations en production
dotnet ef database update --environment Production
```

## Tables de Sécurité Avancée

### EmailVerificationTokens & PasswordResetTokens
Ces tables implémentent un système de sécurité avancé avec :
- **Hash des tokens** : Stockage sécurisé des tokens
- **Limitation des tentatives** : Protection contre le brute-force
- **Traçabilité IP** : Logging des adresses IP pour audit
- **Expiration automatique** : Tokens avec durée de vie limitée
- **Usage unique** : Prévention de la réutilisation

### Optimisations de Performance
- **Plus de 30 indexes** optimisés pour les requêtes fréquentes
- **Indexes géospatiaux** pour les recherches de proximité
- **Indexes composites** pour les filtres complexes
- **Connection pooling** configuré pour MySQL
- **Query splitting** pour les requêtes complexes

### Conformité RGPD
- **Champ IsEmailConfirmed** : Consentement explicite
- **Tokens de vérification** : Double opt-in
- **Traçabilité des accès** : Logs d'audit
- **Droit à l'oubli** : Possibilité de suppression
- **Permissions granulaires** : Contrôle d'accès fin