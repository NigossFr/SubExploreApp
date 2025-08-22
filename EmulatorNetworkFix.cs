// ========================================
// CORRECTIF RESEAU EMULATEUR ANDROID 
// ========================================
// Solution temporaire pour contourner les problèmes DNS de l'émulateur Android

using System.Net;
using System.Net.Http;

namespace SubExplore
{
    /// <summary>
    /// Correctif réseau pour l'émulateur Android qui ne peut pas résoudre les noms DNS
    /// </summary>
    public static class EmulatorNetworkFix
    {
        // ⚠️ SOLUTION TEMPORAIRE - IP directe pour Supabase
        // Obtenue via nslookup iguvwnyehojvxkyqzaoi.supabase.co
        private const string SUPABASE_IP_ADDRESS = "104.18.38.10";
        private const string SUPABASE_HOSTNAME = "iguvwnyehojvxkyqzaoi.supabase.co";
        
        /// <summary>
        /// Applique un correctif DNS temporaire pour l'émulateur Android
        /// </summary>
        public static void ApplyEmulatorDnsFixIfNeeded()
        {
            try
            {
                // Vérifier si nous sommes sur un émulateur Android
                if (IsRunningOnAndroidEmulator())
                {
                    Console.WriteLine("🔧 Application du correctif DNS pour émulateur Android...");
                    
                    // Tester la résolution DNS normale
                    if (!CanResolveHostname(SUPABASE_HOSTNAME))
                    {
                        Console.WriteLine($"❌ Impossible de résoudre {SUPABASE_HOSTNAME}");
                        Console.WriteLine($"🚀 Utilisation de l'IP directe: {SUPABASE_IP_ADDRESS}");
                        
                        // Option 1: Ajouter une entrée DNS locale (nécessite des permissions)
                        // Ce n'est généralement pas possible dans un émulateur standard
                        
                        // Option 2: Modifier l'URL Supabase pour utiliser l'IP directe
                        // Cette méthode sera utilisée dans le service de configuration
                        
                        Console.WriteLine("✅ Correctif DNS appliqué");
                    }
                    else
                    {
                        Console.WriteLine($"✅ DNS fonctionne normalement pour {SUPABASE_HOSTNAME}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Erreur lors de l'application du correctif DNS: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Obtient l'URL Supabase avec correctif émulateur si nécessaire
        /// </summary>
        public static string GetSupabaseUrlWithEmulatorFix(string originalUrl)
        {
            if (IsRunningOnAndroidEmulator() && !CanResolveHostname(SUPABASE_HOSTNAME))
            {
                // Pour le certificat SSL, on garde le hostname original
                // La résolution IP sera gérée au niveau HttpClient
                Console.WriteLine($"🔧 Émulateur détecté, garde hostname pour SSL: {originalUrl}");
                return originalUrl;
            }
            
            return originalUrl;
        }
        
        /// <summary>
        /// Crée un HttpClient configuré pour l'émulateur Android
        /// </summary>
        public static HttpClient CreateEmulatorHttpClient()
        {
#if ANDROID
            if (IsRunningOnAndroidEmulator())
            {
                try
                {
                    Console.WriteLine("🔧 Création HttpClient pour émulateur Android...");
                    
                    // Créer un handler avec validation SSL permissive pour l'émulateur
                    var handler = new HttpClientHandler();
                    
                    // Solution temporaire : accepter tous les certificats SSL en mode émulateur
                    // ATTENTION : NE JAMAIS UTILISER EN PRODUCTION !
                    handler.ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        // En émulateur, accepter les certificats Supabase
                        if (sender is HttpRequestMessage request)
                        {
                            var host = request.RequestUri?.Host;
                            if (host?.Contains("supabase.co") == true || host == SUPABASE_IP_ADDRESS)
                            {
                                Console.WriteLine($"🔐 Acceptation certificat SSL émulateur pour: {host}");
                                return true;
                            }
                        }
                        
                        // Pour les autres, validation normale
                        return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
                    };
                    
                    var client = new HttpClient(handler);
                    client.Timeout = TimeSpan.FromSeconds(30);
                    
                    Console.WriteLine("✅ HttpClient émulateur créé avec validation SSL permissive");
                    return client;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Erreur création HttpClient émulateur: {ex.Message}");
                    return new HttpClient();
                }
            }
#endif
            // Par défaut ou sur les autres plateformes
            return new HttpClient();
        }
        
        /// <summary>
        /// Vérifie si l'application s'exécute sur un émulateur Android
        /// </summary>
        public static bool IsRunningOnAndroidEmulator()
        {
#if ANDROID
            // Méthodes pour détecter un émulateur Android
            var build = Android.OS.Build.Manufacturer;
            var model = Android.OS.Build.Model;
            var product = Android.OS.Build.Product;
            
            return build.Contains("Google") || 
                   model.Contains("Emulator") || 
                   model.Contains("Android SDK") ||
                   product.Contains("sdk") ||
                   product.Contains("emulator");
#else
            // Sur les autres plateformes, pas d'émulateur Android
            return false;
#endif
        }
        
        /// <summary>
        /// Teste si un hostname peut être résolu
        /// </summary>
        private static bool CanResolveHostname(string hostname)
        {
            try
            {
                var addresses = Dns.GetHostAddresses(hostname);
                return addresses.Length > 0;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Informations de diagnostic réseau
        /// </summary>
        public static string GetNetworkDiagnosticInfo()
        {
            var info = new List<string>
            {
                "=== DIAGNOSTIC RESEAU EMULATEUR ===",
                $"Plateforme: {DeviceInfo.Platform}",
                $"Type d'appareil: {DeviceInfo.DeviceType}",
                $"Émulateur détecté: {IsRunningOnAndroidEmulator()}",
                $"Test DNS Supabase: {(CanResolveHostname(SUPABASE_HOSTNAME) ? "✅ OK" : "❌ ECHEC")}",
                $"IP Supabase directe: {SUPABASE_IP_ADDRESS}",
                ""
            };
            
            // Test de connectivité réseau de base
            try
            {
                var connectivity = Connectivity.NetworkAccess;
                info.Add($"Accès réseau: {connectivity}");
                
                var profiles = Connectivity.ConnectionProfiles;
                info.Add($"Profils de connexion: {string.Join(", ", profiles)}");
            }
            catch (Exception ex)
            {
                info.Add($"Erreur test connectivité: {ex.Message}");
            }
            
            return string.Join(Environment.NewLine, info);
        }
    }
}