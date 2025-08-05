using SubExplore.Models.Domain;
using SubExplore.Services.Interfaces;

namespace SubExplore.Services.Templates
{
    /// <summary>
    /// Professional email templates for SubExplore authentication and notifications
    /// </summary>
    public static class EmailTemplates
    {
        #region Email Verification

        /// <summary>
        /// Email verification subject line
        /// </summary>
        public static string GetEmailVerificationSubject() => "🏊‍♀️ Confirmez votre adresse email - SubExplore";

        /// <summary>
        /// Email verification HTML template
        /// </summary>
        public static string GetEmailVerificationTemplate(User user, string verificationUrl)
        {
            return $@"
<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Vérification Email - SubExplore</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #0077FF 0%, #00CC55 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; background: #0077FF; color: white; padding: 15px 30px; text-decoration: none; border-radius: 8px; font-weight: bold; margin: 20px 0; }}
        .button:hover {{ background: #0066DD; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 0.9em; }}
        .security-note {{ background: #E8F4FD; border-left: 4px solid #0077FF; padding: 15px; margin: 20px 0; border-radius: 4px; }}
        .logo {{ font-size: 2em; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='logo'>🏊‍♀️ SubExplore</div>
        <h1>Bienvenue dans la communauté !</h1>
    </div>
    
    <div class='content'>
        <h2>Bonjour {user.FirstName} 👋</h2>
        
        <p>Merci de rejoindre <strong>SubExplore</strong>, votre communauté dédiée aux sports subaquatiques !</p>
        
        <p>Pour activer votre compte et commencer à explorer les plus beaux spots de plongée, merci de confirmer votre adresse email en cliquant sur le bouton ci-dessous :</p>
        
        <div style='text-align: center;'>
            <a href='{verificationUrl}' class='button'>✅ Confirmer mon email</a>
        </div>
        
        <div class='security-note'>
            <strong>🔒 Sécurité :</strong>
            <ul>
                <li>Ce lien expire dans 24 heures</li>
                <li>Si vous n'avez pas créé ce compte, ignorez cet email</li>
                <li>Ne partagez jamais ce lien avec d'autres personnes</li>
            </ul>
        </div>
        
        <p>Une fois votre email confirmé, vous pourrez :</p>
        <ul>
            <li>🗺️ Découvrir des spots de plongée exceptionnels</li>
            <li>📸 Partager vos propres découvertes</li>
            <li>🤝 Rejoindre une communauté passionnée</li>
            <li>⭐ Noter et commenter les spots</li>
        </ul>
        
        <p>Hâte de vous voir explorer les merveilles sous-marines !</p>
        
        <p>L'équipe SubExplore 🌊</p>
    </div>
    
    <div class='footer'>
        <p>Si le bouton ne fonctionne pas, copiez ce lien dans votre navigateur :<br>
        <small>{verificationUrl}</small></p>
        
        <p><small>© 2024 SubExplore - Votre communauté de sports subaquatiques</small></p>
    </div>
</body>
</html>";
        }

        #endregion

        #region Password Reset

        /// <summary>
        /// Password reset subject line
        /// </summary>
        public static string GetPasswordResetSubject() => "🔒 Réinitialisation de votre mot de passe - SubExplore";

        /// <summary>
        /// Password reset HTML template
        /// </summary>
        public static string GetPasswordResetTemplate(User user, string resetUrl)
        {
            return $@"
<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Réinitialisation Mot de Passe - SubExplore</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #FF6B35 0%, #FF8E53 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; background: #FF6B35; color: white; padding: 15px 30px; text-decoration: none; border-radius: 8px; font-weight: bold; margin: 20px 0; }}
        .button:hover {{ background: #E55A2B; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 0.9em; }}
        .security-alert {{ background: #FFF3CD; border-left: 4px solid #FF6B35; padding: 15px; margin: 20px 0; border-radius: 4px; }}
        .warning {{ background: #F8D7DA; border-left: 4px solid #DC3545; padding: 15px; margin: 20px 0; border-radius: 4px; color: #721C24; }}
        .logo {{ font-size: 2em; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='logo'>🔒 SubExplore</div>
        <h1>Réinitialisation de mot de passe</h1>
    </div>
    
    <div class='content'>
        <h2>Bonjour {user.FirstName},</h2>
        
        <p>Nous avons reçu une demande de réinitialisation de mot de passe pour votre compte SubExplore.</p>
        
        <p>Si vous êtes à l'origine de cette demande, cliquez sur le bouton ci-dessous pour créer un nouveau mot de passe :</p>
        
        <div style='text-align: center;'>
            <a href='{resetUrl}' class='button'>🔑 Réinitialiser mon mot de passe</a>
        </div>
        
        <div class='security-alert'>
            <strong>⚠️ Important :</strong>
            <ul>
                <li>Ce lien expire dans <strong>2 heures</strong></li>
                <li>Il ne peut être utilisé qu'une seule fois</li>
                <li>Votre mot de passe actuel reste valide jusqu'à ce que vous en créiez un nouveau</li>
            </ul>
        </div>
        
        <div class='warning'>
            <strong>🚨 Si vous n'avez pas demandé cette réinitialisation :</strong>
            <ul>
                <li>Ignorez cet email - votre compte reste sécurisé</li>
                <li>Votre mot de passe actuel n'a pas changé</li>
                <li>Contactez-nous si vous avez des inquiétudes</li>
            </ul>
        </div>
        
        <p>Pour votre sécurité, nous vous recommandons de choisir un mot de passe :</p>
        <ul>
            <li>🔐 D'au moins 8 caractères</li>
            <li>🔤 Avec majuscules et minuscules</li>
            <li>🔢 Incluant des chiffres</li>
            <li>🔣 Avec des caractères spéciaux</li>
        </ul>
        
        <p>L'équipe Sécurité SubExplore 🛡️</p>
    </div>
    
    <div class='footer'>
        <p>Si le bouton ne fonctionne pas, copiez ce lien dans votre navigateur :<br>
        <small>{resetUrl}</small></p>
        
        <p><small>© 2024 SubExplore - Sécurité et confiance</small></p>
    </div>
</body>
</html>";
        }

        #endregion

        #region Welcome Email

        /// <summary>
        /// Welcome email subject line
        /// </summary>
        public static string GetWelcomeSubject() => "🎉 Bienvenue dans SubExplore ! Votre aventure commence";

        /// <summary>
        /// Welcome email HTML template
        /// </summary>
        public static string GetWelcomeTemplate(User user)
        {
            return $@"
<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Bienvenue - SubExplore</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #00CC55 0%, #0077FF 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; background: #00CC55; color: white; padding: 15px 30px; text-decoration: none; border-radius: 8px; font-weight: bold; margin: 20px 0; }}
        .button:hover {{ background: #00AA44; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 0.9em; }}
        .feature-grid {{ display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin: 20px 0; }}
        .feature-card {{ background: white; padding: 20px; border-radius: 8px; text-align: center; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .logo {{ font-size: 2em; font-weight: bold; }}
        @media (max-width: 600px) {{ .feature-grid {{ grid-template-columns: 1fr; }} }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='logo'>🎉 SubExplore</div>
        <h1>Votre compte est activé !</h1>
    </div>
    
    <div class='content'>
        <h2>Félicitations {user.FirstName} ! 🥳</h2>
        
        <p>Votre adresse email a été confirmée avec succès. Vous faites maintenant partie de la communauté <strong>SubExplore</strong> !</p>
        
        <div style='text-align: center;'>
            <a href='https://subexplore.app' class='button'>🌊 Commencer l'exploration</a>
        </div>
        
        <h3>🗺️ Découvrez ce que vous pouvez faire :</h3>
        
        <div class='feature-grid'>
            <div class='feature-card'>
                <h4>🔍 Explorer</h4>
                <p>Découvrez des milliers de spots de plongée dans le monde entier</p>
            </div>
            <div class='feature-card'>
                <h4>📍 Partager</h4>
                <p>Ajoutez vos spots secrets et aidez la communauté</p>
            </div>
            <div class='feature-card'>
                <h4>⭐ Evaluer</h4>
                <p>Notez et commentez vos expériences de plongée</p>
            </div>
            <div class='feature-card'>
                <h4>🤝 Connecter</h4>
                <p>Rencontrez d'autres passionnés près de chez vous</p>
            </div>
        </div>
        
        <h3>🚀 Premiers pas recommandés :</h3>
        <ol>
            <li>📝 Complétez votre profil avec vos certifications</li>
            <li>🗺️ Explorez la carte interactive des spots</li>
            <li>📸 Ajoutez votre premier spot préféré</li>
            <li>👥 Rejoignez des groupes de plongeurs locaux</li>
        </ol>
        
        <p>N'hésitez pas à nous contacter si vous avez des questions. Notre équipe est là pour vous aider à profiter au maximum de votre expérience SubExplore !</p>
        
        <p>Belles plongées et à bientôt sous l'eau ! 🐠</p>
        
        <p>L'équipe SubExplore 🌊</p>
    </div>
    
    <div class='footer'>
        <p><strong>Besoin d'aide ?</strong><br>
        📧 support@subexplore.app | 🌐 help.subexplore.app</p>
        
        <p><small>© 2024 SubExplore - Votre passeport pour l'exploration subaquatique</small></p>
    </div>
</body>
</html>";
        }

        #endregion

        #region Security Alert

        /// <summary>
        /// Security alert subject line
        /// </summary>
        public static string GetSecurityAlertSubject(SecurityAlertType alertType) => alertType switch
        {
            SecurityAlertType.PasswordChanged => "🔒 Votre mot de passe a été modifié - SubExplore",
            SecurityAlertType.EmailChanged => "📧 Votre email a été modifié - SubExplore",
            SecurityAlertType.AccountLocked => "🚨 Votre compte a été verrouillé - SubExplore",
            SecurityAlertType.SuspiciousLogin => "⚠️ Activité suspecte détectée - SubExplore",
            SecurityAlertType.NewDeviceLogin => "📱 Nouvelle connexion détectée - SubExplore",
            SecurityAlertType.RoleElevated => "⬆️ Vos permissions ont été modifiées - SubExplore",
            SecurityAlertType.AccountDeactivated => "❌ Votre compte a été désactivé - SubExplore",
            _ => "🔔 Alerte sécurité - SubExplore"
        };

        /// <summary>
        /// Security alert HTML template
        /// </summary>
        public static string GetSecurityAlertTemplate(User user, SecurityAlertType alertType, string? details = null)
        {
            var (icon, title, message, severity) = alertType switch
            {
                SecurityAlertType.PasswordChanged => ("🔒", "Mot de passe modifié", "Votre mot de passe a été modifié avec succès.", "info"),
                SecurityAlertType.EmailChanged => ("📧", "Email modifié", "Votre adresse email a été modifiée.", "info"),
                SecurityAlertType.AccountLocked => ("🚨", "Compte verrouillé", "Votre compte a été temporairement verrouillé pour des raisons de sécurité.", "danger"),
                SecurityAlertType.SuspiciousLogin => ("⚠️", "Activité suspecte", "Une activité suspecte a été détectée sur votre compte.", "warning"),
                SecurityAlertType.NewDeviceLogin => ("📱", "Nouvelle connexion", "Une connexion depuis un nouvel appareil a été détectée.", "info"),
                SecurityAlertType.RoleElevated => ("⬆️", "Permissions modifiées", "Vos permissions de compte ont été mises à jour.", "info"),
                SecurityAlertType.AccountDeactivated => ("❌", "Compte désactivé", "Votre compte a été désactivé.", "danger"),
                _ => ("🔔", "Alerte sécurité", "Une activité importante a eu lieu sur votre compte.", "info")
            };

            var backgroundColor = severity switch
            {
                "danger" => "#DC3545",
                "warning" => "#FFC107",
                _ => "#0077FF"
            };

            return $@"
<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Alerte Sécurité - SubExplore</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: {backgroundColor}; color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 0.9em; }}
        .alert-info {{ background: #E8F4FD; border-left: 4px solid #0077FF; padding: 15px; margin: 20px 0; border-radius: 4px; }}
        .logo {{ font-size: 2em; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='logo'>{icon} SubExplore</div>
        <h1>{title}</h1>
    </div>
    
    <div class='content'>
        <h2>Bonjour {user.FirstName},</h2>
        
        <p>{message}</p>
        
        {(string.IsNullOrEmpty(details) ? "" : $"<p><strong>Détails :</strong> {details}</p>")}
        
        <div class='alert-info'>
            <strong>📅 Date :</strong> {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC<br>
            <strong>👤 Compte :</strong> {user.Email}<br>
            <strong>🆔 ID Utilisateur :</strong> {user.Id}
        </div>
        
        <p>Si cette action n'a pas été effectuée par vous, contactez immédiatement notre équipe de sécurité.</p>
        
        <p>L'équipe Sécurité SubExplore 🛡️</p>
    </div>
    
    <div class='footer'>
        <p><strong>Besoin d'aide ?</strong><br>
        📧 security@subexplore.app | 🆘 Urgence : +33 1 XX XX XX XX</p>
        
        <p><small>© 2024 SubExplore - Sécurité et confiance</small></p>
    </div>
</body>
</html>";
        }

        #endregion

        #region Test Email

        /// <summary>
        /// Test email subject line
        /// </summary>
        public static string GetTestEmailSubject() => "✅ Test de configuration email - SubExplore";

        /// <summary>
        /// Test email HTML template
        /// </summary>
        public static string GetTestEmailTemplate()
        {
            return $@"
<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Test Email - SubExplore</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #28A745 0%, #20C997 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 0.9em; }}
        .success-box {{ background: #D4EDDA; border-left: 4px solid #28A745; padding: 15px; margin: 20px 0; border-radius: 4px; color: #155724; }}
        .logo {{ font-size: 2em; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='logo'>✅ SubExplore</div>
        <h1>Test Email Réussi !</h1>
    </div>
    
    <div class='content'>
        <h2>Configuration Email Validée 🎉</h2>
        
        <p>Félicitations ! Ce message confirme que votre configuration email SubExplore fonctionne parfaitement.</p>
        
        <div class='success-box'>
            <strong>✅ Tests validés :</strong>
            <ul>
                <li>Connexion SMTP établie</li>
                <li>Authentification réussie</li>
                <li>Envoi d'email fonctionnel</li>
                <li>Templates HTML compatibles</li>
            </ul>
        </div>
        
        <h3>📊 Informations techniques :</h3>
        <ul>
            <li><strong>Date/Heure :</strong> {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC</li>
            <li><strong>Version :</strong> SubExplore Email Service v1.0</li>
            <li><strong>Encodage :</strong> UTF-8</li>
            <li><strong>Format :</strong> HTML + Text</li>
        </ul>
        
        <p>Votre service email est maintenant prêt pour :</p>
        <ul>
            <li>🔗 Vérifications d'email</li>
            <li>🔒 Réinitialisations de mot de passe</li>
            <li>🎉 Messages de bienvenue</li>
            <li>🔔 Alertes de sécurité</li>
        </ul>
        
        <p>L'équipe Technique SubExplore 🛠️</p>
    </div>
    
    <div class='footer'>
        <p><small>© 2024 SubExplore - Service Email Opérationnel</small></p>
    </div>
</body>
</html>";
        }

        #endregion
    }
}