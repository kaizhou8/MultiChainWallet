using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 安全的配置管理器
    /// Secure configuration manager
    /// </summary>
    public class SecureConfigManager
    {
        private readonly string _configDirectory;
        private readonly string _configFilePath;
        private readonly EnhancedSecurityService _securityService;
        private readonly ILogger<SecureConfigManager> _logger;
        private readonly byte[] _configEncryptionKey;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public SecureConfigManager(
            EnhancedSecurityService securityService,
            ILogger<SecureConfigManager> logger)
        {
            _securityService = securityService;
            _logger = logger;
            
            _configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MultiChainWallet",
                "Config"
            );
            Directory.CreateDirectory(_configDirectory);
            
            _configFilePath = Path.Combine(_configDirectory, "secure_config.dat");
            
            // 生成或加载配置加密密钥
            // Generate or load configuration encryption key
            _configEncryptionKey = GetOrCreateConfigEncryptionKey();
        }

        /// <summary>
        /// 保存安全配置
        /// Save secure configuration
        /// </summary>
        public async Task SaveSecureConfigAsync<T>(T config, string masterPassword) where T : class
        {
            try
            {
                // 序列化配置
                // Serialize configuration
                string jsonConfig = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                // 使用DPAPI加密配置
                // Encrypt configuration using DPAPI
                byte[] encryptedConfig = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(jsonConfig),
                    _configEncryptionKey,
                    DataProtectionScope.CurrentUser);
                
                // 使用主密码进行额外加密
                // Additional encryption with master password
                if (!string.IsNullOrEmpty(masterPassword))
                {
                    using var aes = Aes.Create();
                    using var deriveBytes = new Rfc2898DeriveBytes(
                        masterPassword,
                        _configEncryptionKey,
                        600000,
                        HashAlgorithmName.SHA256);
                    
                    aes.Key = deriveBytes.GetBytes(32);
                    aes.IV = deriveBytes.GetBytes(16);
                    
                    using var msEncrypt = new MemoryStream();
                    msEncrypt.Write(aes.IV, 0, aes.IV.Length); // 保存IV / Save IV
                    
                    using (var cryptoStream = new CryptoStream(
                        msEncrypt,
                        aes.CreateEncryptor(),
                        CryptoStreamMode.Write))
                    {
                        await cryptoStream.WriteAsync(encryptedConfig, 0, encryptedConfig.Length);
                    }
                    
                    encryptedConfig = msEncrypt.ToArray();
                }
                
                // 写入文件
                // Write to file
                await File.WriteAllBytesAsync(_configFilePath, encryptedConfig);
                
                _logger.LogInformation("安全配置已保存 / Secure configuration saved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存安全配置失败 / Failed to save secure configuration");
                throw new InvalidOperationException("保存安全配置失败 / Failed to save secure configuration", ex);
            }
        }

        /// <summary>
        /// 加载安全配置
        /// Load secure configuration
        /// </summary>
        public async Task<T> LoadSecureConfigAsync<T>(string masterPassword) where T : class, new()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    _logger.LogWarning("安全配置文件不存在，返回默认配置 / Secure configuration file does not exist, returning default configuration");
                    return new T();
                }
                
                // 读取加密配置
                // Read encrypted configuration
                byte[] encryptedConfig = await File.ReadAllBytesAsync(_configFilePath);
                
                // 使用主密码解密
                // Decrypt with master password
                if (!string.IsNullOrEmpty(masterPassword))
                {
                    using var aes = Aes.Create();
                    using var deriveBytes = new Rfc2898DeriveBytes(
                        masterPassword,
                        _configEncryptionKey,
                        600000,
                        HashAlgorithmName.SHA256);
                    
                    aes.Key = deriveBytes.GetBytes(32);
                    
                    // 读取IV
                    // Read IV
                    byte[] iv = new byte[16];
                    Array.Copy(encryptedConfig, 0, iv, 0, iv.Length);
                    aes.IV = iv;
                    
                    using var msDecrypt = new MemoryStream();
                    using (var cryptoStream = new CryptoStream(
                        msDecrypt,
                        aes.CreateDecryptor(),
                        CryptoStreamMode.Write))
                    {
                        await cryptoStream.WriteAsync(encryptedConfig, iv.Length, encryptedConfig.Length - iv.Length);
                    }
                    
                    encryptedConfig = msDecrypt.ToArray();
                }
                
                // 使用DPAPI解密
                // Decrypt using DPAPI
                byte[] decryptedConfig = ProtectedData.Unprotect(
                    encryptedConfig,
                    _configEncryptionKey,
                    DataProtectionScope.CurrentUser);
                
                // 反序列化配置
                // Deserialize configuration
                string jsonConfig = Encoding.UTF8.GetString(decryptedConfig);
                return JsonSerializer.Deserialize<T>(jsonConfig);
            }
            catch (CryptographicException)
            {
                _logger.LogWarning("解密配置失败，可能是密码错误 / Failed to decrypt configuration, possibly wrong password");
                throw new InvalidOperationException("解密配置失败，请检查密码 / Failed to decrypt configuration, please check your password");
            }
            catch (JsonException)
            {
                _logger.LogWarning("配置格式无效，返回默认配置 / Invalid configuration format, returning default configuration");
                return new T();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载安全配置失败 / Failed to load secure configuration");
                throw new InvalidOperationException("加载安全配置失败 / Failed to load secure configuration", ex);
            }
        }

        /// <summary>
        /// 重置安全配置
        /// Reset secure configuration
        /// </summary>
        public async Task ResetSecureConfigAsync()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    // 安全删除文件
                    // Securely delete file
                    long fileSize = new FileInfo(_configFilePath).Length;
                    byte[] randomData = new byte[fileSize];
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        rng.GetBytes(randomData);
                    }
                    
                    await File.WriteAllBytesAsync(_configFilePath, randomData);
                    File.Delete(_configFilePath);
                    
                    _logger.LogInformation("安全配置已重置 / Secure configuration reset");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置安全配置失败 / Failed to reset secure configuration");
                throw new InvalidOperationException("重置安全配置失败 / Failed to reset secure configuration", ex);
            }
        }

        /// <summary>
        /// 获取或创建配置加密密钥
        /// Get or create configuration encryption key
        /// </summary>
        private byte[] GetOrCreateConfigEncryptionKey()
        {
            string keyFilePath = Path.Combine(_configDirectory, "config.key");
            
            try
            {
                if (File.Exists(keyFilePath))
                {
                    // 使用DPAPI解密密钥
                    // Decrypt key using DPAPI
                    byte[] encryptedKey = File.ReadAllBytes(keyFilePath);
                    return ProtectedData.Unprotect(
                        encryptedKey,
                        null,
                        DataProtectionScope.CurrentUser);
                }
                
                // 创建新密钥
                // Create new key
                byte[] key = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(key);
                }
                
                // 使用DPAPI加密密钥
                // Encrypt key using DPAPI
                byte[] encryptedKey = ProtectedData.Protect(
                    key,
                    null,
                    DataProtectionScope.CurrentUser);
                
                File.WriteAllBytes(keyFilePath, encryptedKey);
                
                return key;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取或创建配置加密密钥失败 / Failed to get or create configuration encryption key");
                
                // 在出错时生成临时密钥
                // Generate temporary key on error
                byte[] tempKey = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(tempKey);
                }
                
                return tempKey;
            }
        }
    }
} 