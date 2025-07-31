# 🌊 SubExplore - Workflow de Validation des Spots

## Vue d'ensemble

Le système de validation des spots de SubExplore permet aux administrateurs et modérateurs experts de gérer la qualité et la sécurité des spots proposés par la communauté.

## 📋 États de validation

| État | Valeur | Description | Actions disponibles |
|------|--------|-------------|-------------------|
| **Draft** | 0 | Brouillon en cours de création | Continuer l'édition, Soumettre |
| **Pending** | 1 | En attente de validation | Assigner, Approuver, Rejeter |
| **UnderReview** | 2 | En cours de révision | Approuver, Rejeter, Révision sécurité |
| **NeedsRevision** | 3 | Nécessite des modifications | Éditer, Resoumettre |
| **SafetyReview** | 4 | Révision de sécurité requise | Marquer sûr, Confirmer dangereux |
| **Approved** | 5 | Spot validé et publié | Archiver, Signaler |
| **Rejected** | 6 | Spot rejeté | Réviser et resoumettre |
| **Archived** | 7 | Spot archivé | Restaurer |

## 👥 Système de rôles

### Utilisateurs standards
- **Statut initial**: `Pending` (En attente de validation)
- **Permissions**: Créer des spots, modifier leurs propres spots
- **Workflow**: Soumission → Validation par modérateur → Publication

### Modérateurs experts (ExpertModerator)
- **Statut initial**: `Approved` (Auto-approuvé)
- **Permissions**: Valider, approuver, rejeter tous les spots
- **Spécialisations**: Plongée technique, apnée, snorkeling, etc.

### Administrateurs
- **Statut initial**: `Approved` (Auto-approuvé)
- **Permissions**: Toutes les actions de validation + gestion des utilisateurs
- **Accès**: Interface d'administration complète

## 🎯 Interface d'administration

### Accès au menu admin
1. **Connexion** avec un compte Administrator ou ExpertModerator
2. **Menu latéral** → Section "Administration"
3. **Cliquer** sur "Validation des Spots"

### Onglets de validation

#### 📋 En attente (Pending)
- **Spots** nouvellement soumis par les utilisateurs
- **Actions**: Assigner pour révision, Approuver directement, Rejeter
- **Tri**: Par date de création (plus anciens en premier)

#### 🔍 En révision (UnderReview)  
- **Spots** assignés à un modérateur pour révision détaillée
- **Actions**: Approuver, Rejeter, Marquer pour révision sécurité
- **Suivi**: Notes de validation, historique des actions

#### ⚠️ Révision sécurité (SafetyReview)
- **Spots** nécessitant une vérification de sécurité
- **Actions**: Marquer comme sûr, Confirmer dangereux
- **Critères**: Profondeur, courants, équipement requis

## 🔄 Flux de validation

### Flux standard
```
Soumission → Pending → UnderReview → Approved ✅
              ↓
            Rejected ❌
```

### Flux avec révision sécurité
```
Pending → SafetyReview → Approved ✅
                    ↓
                 Rejected ❌
```

### Flux de révision
```
UnderReview → NeedsRevision → (Utilisateur modifie) → Pending
```

## 📊 Statistiques de validation

L'interface affiche en temps réel:
- **Spots en attente**: Nombre de spots à traiter
- **Spots en révision**: Nombre de spots assignés
- **Spots en sécurité**: Nombre de spots à vérifier
- **Taux d'approbation**: Pourcentage d'approbation sur 30 jours

## 🛠️ Actions de validation

### Approuver un spot
1. **Sélectionner** le spot dans la liste
2. **Vérifier** les informations (nom, description, coordonnées)
3. **Ajouter des notes** de validation (optionnel)
4. **Cliquer** "Approuver"
5. **Confirmation** → Spot publié sur la carte

### Rejeter un spot
1. **Sélectionner** le spot problématique
2. **Saisir la raison** du rejet dans les notes
3. **Cliquer** "Rejeter"
4. **Notification** automatique à l'utilisateur

### Assigner pour révision
1. **Spot complexe** nécessitant une analyse approfondie
2. **Cliquer** "Assigner pour révision"
3. **Passage** à l'onglet "En révision"
4. **Révision détaillée** par le modérateur

## 🔐 Sécurité et permissions

### Contrôle d'accès
- **Vérification** automatique des permissions à l'ouverture
- **Redirection** si l'utilisateur n'a pas les droits
- **Logs** de toutes les actions de validation

### Audit trail
- **Historique** complet des actions de validation
- **Traçabilité** : qui a validé quoi et quand
- **Notes** de validation conservées

## 🚀 Migration et données de test

### Migration automatique
Le système inclut une migration automatique qui :
- **Met à jour** les spots existants des admins vers le statut `Approved`
- **Crée** des spots de test pour valider le workflow
- **Vérifie** la cohérence des données

### Spots de test créés
- **[TEST VALIDATION] Spot en attente de validation** (Status: Pending)
- **[TEST] Épave du Donator - En attente** (Status: Pending)
- **[TEST] Tombant de la Cassidaigne - Révision** (Status: NeedsRevision)
- **[TEST] Grotte bleue - Sécurité** (Status: SafetyReview)
- **[TEST] Récif des Moyades - En cours** (Status: UnderReview)

## 📱 Interface utilisateur

### Design responsive
- **Interface** adaptée aux tablettes et écrans desktop
- **Navigation** par onglets pour organiser les spots
- **Panneau** de détails avec toutes les informations du spot
- **Actions** contextuelles selon le statut du spot

### Indicateurs visuels
- **Couleurs** distinctes pour chaque statut
- **Badges** colorés pour identification rapide
- **Statistiques** en temps réel dans l'en-tête

## 🔧 Dépannage

### Le menu admin n'apparaît pas
1. **Vérifier** que l'utilisateur est connecté
2. **Confirmer** le type de compte (Administrator/ExpertModerator)
3. **Redémarrer** l'application si nécessaire

### Spots non visibles sur la carte
1. **Vérifier** le statut de validation (seuls les spots `Approved` apparaissent)
2. **Utiliser** l'interface admin pour approuver les spots
3. **Actualiser** la carte après approbation

### Erreurs de migration
1. **Consulter** les logs de l'application
2. **Vérifier** la connexion à la base de données
3. **Relancer** l'application pour réessayer la migration

## 📞 Support technique

En cas de problème avec le système de validation :
1. **Consulter** les logs de debug de l'application
2. **Vérifier** l'état de la base de données
3. **Contacter** l'équipe technique avec les détails du problème

---

*Documentation générée automatiquement pour SubExplore v1.0*
*Dernière mise à jour : Workflow de validation des spots implémenté*