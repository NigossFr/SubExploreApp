using SubExplore.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SubExplore.Services.Implementations
{
    /// <summary>
    /// Secure settings service with encryption for sensitive data storage
    /// Extends SettingsService with secure storage capabilities
    /// </summary>
    public class SecureSettingsService : ISecureSettingsService
    {
        private readonly ISettingsService _baseSettingsService;
        private readonly byte[] _encryptionKey;
        private const string ACCESS_TOKEN_KEY = "secure_access_token";
        private const string REFRESH_TOKEN_KEY = "secure_refresh_token";

        public SecureSettingsService(ISettingsService baseSettingsService)
        {
            _baseSettingsService = baseSettingsService;
            _encryptionKey = GetOrCreateEncryptionKey();
        }

        #region ISettingsService Implementation (Delegate to base service)

        public void Set<T>(string key, T value)
        {
            _baseSettingsService.Set(key, value);
        }

        public T Get<T>(string key, T defaultValue = default)
        {
            return _baseSettingsService.Get(key, defaultValue);
        }

        public bool Contains(string key)
        {
            return _baseSettingsService.Contains(key);
        }

        public void Remove(string key)
        {
            _baseSettingsService.Remove(key);
        }

        public void Clear()
        {
            _baseSettingsService.Clear();
        }

        #endregion

        #region Secure Storage Implementation

        public async Task SetSecureAsync<T>(string key, T value)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                var encryptedData = await EncryptDataAsync(json);
                _baseSettingsService.Set($"secure_{key}", encryptedData);

                System.Diagnostics.Debug.WriteLine($"[SecureSettings] Stored secure value for key: {key}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureSettings] Error storing secure value for key '{key}': {ex.Message}");
                throw;
            }
        }

        public async Task<T> GetSecureAsync<T>(string key, T defaultValue = default)
        {
            try
            {
                var secureKey = $"secure_{key}";
                if (!_baseSettingsService.Contains(secureKey))
                {
                    return defaultValue;
                }

                var encryptedData = _baseSettingsService.Get<string>(secureKey);
                if (string.IsNullOrEmpty(encryptedData))
                {
                    return defaultValue;
                }

                var decryptedJson = await DecryptDataAsync(encryptedData);
                var value = JsonSerializer.Deserialize<T>(decryptedJson);
                return value ?? defaultValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureSettings] Error retrieving secure value for key '{key}': {ex.Message}");
                return defaultValue;
            }
        }

        public async Task<bool> ContainsSecureAsync(string key)
        {
            try
            {
                var secureKey = $"secure_{key}";
                return _baseSettingsService.Contains(secureKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureSettings] Error checking secure key '{key}': {ex.Message}");
                return false;
            }
        }

        public async Task RemoveSecureAsync(string key)
        {
            try
            {
                var secureKey = $"secure_{key}";
                _baseSettingsService.Remove(secureKey);
                System.Diagnostics.Debug.WriteLine($"[SecureSettings] Removed secure value for key: {key}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureSettings] Error removing secure key '{key}': {ex.Message}");
                throw;
            }
        }

        public async Task ClearSecureAsync()
        {
            try
            {
                // Get all keys and remove secure ones
                var allKeys = new List<string>();
                
                // Since ISettingsService doesn't expose key enumeration,
                // we'll track known secure keys
                var knownSecureKeys = new[]
                {
                    ACCESS_TOKEN_KEY,
                    REFRESH_TOKEN_KEY,
                    "secure_RevokedTokens",
                    "secure_CurrentUserId"
                };

                foreach (var key in knownSecureKeys)
                {
                    if (_baseSettingsService.Contains(key))
                    {
                        _baseSettingsService.Remove(key);
                    }
                }

                System.Diagnostics.Debug.WriteLine("[SecureSettings] Cleared all secure data");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureSettings] Error clearing secure data: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Authentication Token Methods

        public async Task SetAccessTokenAsync(string token)
        {
            await SetSecureAsync(ACCESS_TOKEN_KEY, token);
        }

        public async Task<string?> GetAccessTokenAsync()
        {
            return await GetSecureAsync<string?>(ACCESS_TOKEN_KEY);
        }

        public async Task SetRefreshTokenAsync(string token)
        {
            await SetSecureAsync(REFRESH_TOKEN_KEY, token);
        }

        public async Task<string?> GetRefreshTokenAsync()
        {
            return await GetSecureAsync<string?>(REFRESH_TOKEN_KEY);
        }

        public async Task ClearAuthenticationTokensAsync()
        {
            await RemoveSecureAsync(ACCESS_TOKEN_KEY);
            await RemoveSecureAsync(REFRESH_TOKEN_KEY);
            System.Diagnostics.Debug.WriteLine("[SecureSettings] Cleared authentication tokens");
        }

        #endregion

        #region Encryption/Decryption

        private async Task<string> EncryptDataAsync(string plainText)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = _encryptionKey;
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor();
                using var msEncrypt = new MemoryStream();
                using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
                using var swEncrypt = new StreamWriter(csEncrypt);

                await swEncrypt.WriteAsync(plainText);
                await swEncrypt.FlushAsync();
                await csEncrypt.FlushFinalBlockAsync();

                var iv = aes.IV;
                var encryptedBytes = msEncrypt.ToArray();
                var result = new byte[iv.Length + encryptedBytes.Length];
                Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                Buffer.BlockCopy(encryptedBytes, 0, result, iv.Length, encryptedBytes.Length);

                return Convert.ToBase64String(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureSettings] Encryption failed: {ex.Message}");
                throw new CryptographicException("Failed to encrypt data", ex);
            }
        }

        private async Task<string> DecryptDataAsync(string cipherText)
        {
            try
            {
                var fullCipher = Convert.FromBase64String(cipherText);

                using var aes = Aes.Create();
                aes.Key = _encryptionKey;

                var iv = new byte[aes.BlockSize / 8];
                var cipher = new byte[fullCipher.Length - iv.Length];

                Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                using var msDecrypt = new MemoryStream(cipher);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);

                return await srDecrypt.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureSettings] Decryption failed: {ex.Message}");
                throw new CryptographicException("Failed to decrypt data", ex);
            }
        }

        private byte[] GetOrCreateEncryptionKey()
        {
            try
            {
                const string keyName = "encryption_key";
                
                if (_baseSettingsService.Contains(keyName))
                {
                    var existingKeyBase64 = _baseSettingsService.Get<string>(keyName);
                    if (!string.IsNullOrEmpty(existingKeyBase64))
                    {
                        return Convert.FromBase64String(existingKeyBase64);
                    }
                }

                // Generate new key
                using var aes = Aes.Create();
                aes.GenerateKey();
                var newKey = aes.Key;
                
                // Store the key
                _baseSettingsService.Set(keyName, Convert.ToBase64String(newKey));
                
                System.Diagnostics.Debug.WriteLine("[SecureSettings] Generated new encryption key");
                return newKey;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureSettings] Error managing encryption key: {ex.Message}");
                throw;
            }
        }

        #endregion
    }
}