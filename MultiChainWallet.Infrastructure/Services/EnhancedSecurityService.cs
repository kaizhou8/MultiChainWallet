using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Services;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 增强的安全服务类
    /// Enhanced security service class
    /// </summary>
    public class EnhancedSecurityService
    {
        private const int PBKDF2_ITERATIONS = 600000; // 高迭代次数以增加安全性 / High iteration count for security
        private const int SALT_SIZE = 32;
        private const int KEY_SIZE = 32;
        private readonly string _securityDirectory;
        private readonly ILogger<EnhancedSecurityService> _logger;
        private readonly ICryptoService _cryptoService;
        private readonly IConfiguration _configuration;
        private readonly byte[] _masterKeyEncryptionKey;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public EnhancedSecurityService(
            ILogger<EnhancedSecurityService> logger,
            ICryptoService cryptoService,
            IConfiguration configuration)
        {
            _logger = logger;
            _cryptoService = cryptoService;
            _configuration = configuration;
            
            _securityDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MultiChainWallet",
                "Security"
            );
            Directory.CreateDirectory(_securityDirectory);
            
            // 初始化主密钥加密密钥
            // Initialize master key encryption key
            string masterKeyBase64 = _configuration["Security:MasterKeyEncryptionKey"];
            if (string.IsNullOrEmpty(masterKeyBase64))
            {
                _masterKeyEncryptionKey = new byte[KEY_SIZE];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(_masterKeyEncryptionKey);
                
                // 在实际应用中，应该将这个密钥安全地存储在配置或安全存储中
                // In a real application, this key should be securely stored in configuration or secure storage
                _logger.LogWarning("生成了新的主密钥加密密钥，应该将其安全地存储 / Generated new master key encryption key, should be securely stored");
            }
            else
            {
                _masterKeyEncryptionKey = Convert.FromBase64String(masterKeyBase64);
            }
        }

        /// <summary>
        /// 加密私钥 - 使用主密钥和用户密码的双重保护
        /// Encrypt private key - using dual protection with master key and user password
        /// </summary>
        public async Task<string> EncryptPrivateKeyAsync(string privateKey, string password)
        {
            try
            {
                // 第一层：使用用户密码加密
                // First layer: encrypt with user password
                byte[] firstLayerEncrypted = _cryptoService.Encrypt(privateKey, password);
                
                // 第二层：使用主密钥加密
                // Second layer: encrypt with master key
                string secondLayerData = Convert.ToBase64String(firstLayerEncrypted);
                byte[] secondLayerEncrypted = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(secondLayerData),
                    _masterKeyEncryptionKey,
                    DataProtectionScope.CurrentUser);
                
                return Convert.ToBase64String(secondLayerEncrypted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加密私钥时发生错误 / Error occurred while encrypting private key");
                throw new InvalidOperationException("加密私钥失败 / Failed to encrypt private key", ex);
            }
        }

        /// <summary>
        /// 解密私钥
        /// Decrypt private key
        /// </summary>
        public async Task<string> DecryptPrivateKeyAsync(string encryptedPrivateKey, string password)
        {
            try
            {
                // 第二层：使用主密钥解密
                // Second layer: decrypt with master key
                byte[] secondLayerEncrypted = Convert.FromBase64String(encryptedPrivateKey);
                byte[] secondLayerDecrypted = ProtectedData.Unprotect(
                    secondLayerEncrypted,
                    _masterKeyEncryptionKey,
                    DataProtectionScope.CurrentUser);
                string firstLayerEncrypted = Encoding.UTF8.GetString(secondLayerDecrypted);
                
                // 第一层：使用用户密码解密
                // First layer: decrypt with user password
                byte[] firstLayerData = Convert.FromBase64String(firstLayerEncrypted);
                return _cryptoService.Decrypt(firstLayerData, password);
            }
            catch (CryptographicException)
            {
                _logger.LogWarning("解密私钥失败，可能是密码错误 / Failed to decrypt private key, possibly wrong password");
                throw new InvalidOperationException("解密私钥失败，请检查密码 / Failed to decrypt private key, please check your password");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解密私钥时发生错误 / Error occurred while decrypting private key");
                throw new InvalidOperationException("解密私钥失败 / Failed to decrypt private key", ex);
            }
        }

        /// <summary>
        /// 验证密码强度
        /// Validate password strength
        /// </summary>
        public (bool IsValid, string ErrorMessage) ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 12)
            {
                return (false, "密码长度必须至少为12个字符 / Password must be at least 12 characters long");
            }

            bool hasLower = false, hasUpper = false, hasDigit = false, hasSpecial = false;
            
            foreach (char c in password)
            {
                if (char.IsLower(c)) hasLower = true;
                else if (char.IsUpper(c)) hasUpper = true;
                else if (char.IsDigit(c)) hasDigit = true;
                else hasSpecial = true;
            }

            if (!hasLower || !hasUpper || !hasDigit || !hasSpecial)
            {
                return (false, "密码必须包含大写字母、小写字母、数字和特殊字符 / Password must contain uppercase letters, lowercase letters, numbers, and special characters");
            }

            // 检查常见密码列表（在实际应用中应该有一个更完整的列表）
            // Check against common password list (should have a more complete list in a real application)
            string[] commonPasswords = { "Password123!", "Qwerty123!", "Admin123!" };
            foreach (var commonPwd in commonPasswords)
            {
                if (password == commonPwd)
                {
                    return (false, "密码太常见，请选择更独特的密码 / Password is too common, please choose a more unique password");
                }
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// 生成安全会话令牌
        /// Generate secure session token
        /// </summary>
        public string GenerateSessionToken()
        {
            byte[] tokenBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(tokenBytes);
            return Convert.ToBase64String(tokenBytes);
        }

        /// <summary>
        /// 创建安全的临时文件
        /// Create secure temporary file
        /// </summary>
        public string CreateSecureTempFile(byte[] data)
        {
            string tempFilePath = Path.Combine(_securityDirectory, $"temp_{Guid.NewGuid():N}.dat");
            
            try
            {
                // 使用DPAPI加密数据
                // Encrypt data using DPAPI
                byte[] encryptedData = ProtectedData.Protect(
                    data,
                    _masterKeyEncryptionKey,
                    DataProtectionScope.CurrentUser);
                
                File.WriteAllBytes(tempFilePath, encryptedData);
                return tempFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建安全临时文件失败 / Failed to create secure temporary file");
                throw;
            }
        }

        /// <summary>
        /// 读取安全的临时文件
        /// Read secure temporary file
        /// </summary>
        public byte[] ReadSecureTempFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("临时文件不存在 / Temporary file does not exist", filePath);
                }
                
                byte[] encryptedData = File.ReadAllBytes(filePath);
                
                // 使用DPAPI解密数据
                // Decrypt data using DPAPI
                return ProtectedData.Unprotect(
                    encryptedData,
                    _masterKeyEncryptionKey,
                    DataProtectionScope.CurrentUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取安全临时文件失败 / Failed to read secure temporary file");
                throw;
            }
        }

        /// <summary>
        /// 删除安全的临时文件
        /// Delete secure temporary file
        /// </summary>
        public void DeleteSecureTempFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    // 在删除前用随机数据覆盖文件
                    // Overwrite file with random data before deleting
                    long fileSize = new FileInfo(filePath).Length;
                    byte[] randomData = new byte[fileSize];
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        rng.GetBytes(randomData);
                    }
                    
                    File.WriteAllBytes(filePath, randomData);
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除安全临时文件失败 / Failed to delete secure temporary file");
                // 不抛出异常，因为这是清理操作
                // Don't throw exception as this is a cleanup operation
            }
        }

        /// <summary>
        /// 安全地清除内存中的敏感数据
        /// Securely clear sensitive data from memory
        /// </summary>
        public void SecurelyClearMemory(byte[] sensitiveData)
        {
            if (sensitiveData != null)
            {
                // 用随机数据覆盖
                // Overwrite with random data
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(sensitiveData);
                }
                
                // 用零覆盖
                // Overwrite with zeros
                Array.Clear(sensitiveData, 0, sensitiveData.Length);
            }
        }
    }
} 