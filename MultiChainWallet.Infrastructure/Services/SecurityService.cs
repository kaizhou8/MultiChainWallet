using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Interfaces;
using MultiChainWallet.Core.Services.Security;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 安全服务实现类
    /// Security service implementation
    /// </summary>
    public class SecurityService : ISecurityService
    {
        private const int PBKDF2_ITERATIONS = 600000; // 高迭代次数以增加安全性 / High iteration count for security
        private const int SALT_SIZE = 32;
        private const int KEY_SIZE = 32;
        private readonly string _securityDirectory;
        private readonly ILogger<SecurityService> _logger;
        private readonly CustomSecurityService _customSecurityService;
        private readonly int _minPasswordLength = 12;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="logger">日志记录器 / Logger</param>
        public SecurityService(ILogger<SecurityService> logger)
        {
            _logger = logger;
            _customSecurityService = new CustomSecurityService(_logger);
            _securityDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MultiChainWallet",
                "Security"
            );
            Directory.CreateDirectory(_securityDirectory);
        }

        /// <summary>
        /// 加密私钥
        /// Encrypt private key
        /// </summary>
        public async Task<string> EncryptPrivateKeyAsync(string privateKey, string password)
        {
            // 生成随机盐
            // Generate random salt
            byte[] salt = new byte[SALT_SIZE];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // 使用PBKDF2派生密钥
            // Derive key using PBKDF2
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                PBKDF2_ITERATIONS,
                HashAlgorithmName.SHA256);
            byte[] key = pbkdf2.GetBytes(KEY_SIZE);

            // 加密私钥
            // Encrypt private key
            byte[] encryptedData;
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();

                using var msEncrypt = new MemoryStream();
                // 写入盐和IV
                // Write salt and IV
                await msEncrypt.WriteAsync(salt, 0, salt.Length);
                await msEncrypt.WriteAsync(aes.IV, 0, aes.IV.Length);

                using (var cryptoStream = new CryptoStream(
                    msEncrypt,
                    aes.CreateEncryptor(),
                    CryptoStreamMode.Write))
                using (var writer = new StreamWriter(cryptoStream))
                {
                    await writer.WriteAsync(privateKey);
                }

                encryptedData = msEncrypt.ToArray();
            }

            return Convert.ToBase64String(encryptedData);
        }

        /// <summary>
        /// 解密私钥
        /// Decrypt private key
        /// </summary>
        public async Task<string> DecryptPrivateKeyAsync(string encryptedPrivateKey, string password)
        {
            byte[] encryptedData = Convert.FromBase64String(encryptedPrivateKey);

            // 读取盐和IV
            // Read salt and IV
            byte[] salt = new byte[SALT_SIZE];
            byte[] iv = new byte[16]; // AES块大小 / AES block size
            Array.Copy(encryptedData, 0, salt, 0, salt.Length);
            Array.Copy(encryptedData, salt.Length, iv, 0, iv.Length);

            // 使用PBKDF2派生密钥
            // Derive key using PBKDF2
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                PBKDF2_ITERATIONS,
                HashAlgorithmName.SHA256);
            byte[] key = pbkdf2.GetBytes(KEY_SIZE);

            // 解密私钥
            // Decrypt private key
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var msDecrypt = new MemoryStream(
                encryptedData,
                salt.Length + iv.Length,
                encryptedData.Length - salt.Length - iv.Length);
            using var cryptoStream = new CryptoStream(
                msDecrypt,
                aes.CreateDecryptor(),
                CryptoStreamMode.Read);
            using var reader = new StreamReader(cryptoStream);

            return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// 加密数据
        /// Encrypt data
        /// </summary>
        /// <param name="data">要加密的数据 / Data to encrypt</param>
        /// <param name="password">密码 / Password</param>
        /// <returns>加密后的数据 / Encrypted data</returns>
        public async Task<byte[]> EncryptAsync(byte[] data, string password)
        {
            try
            {
                // 验证运行时安全环境 / Verify runtime security environment
                if (!_customSecurityService.VerifyRuntimeSecurity())
                {
                    _logger.LogWarning("检测到不安全的运行环境，加密操作可能不安全 / Detected insecure runtime environment, encryption operation may not be secure");
                }

                // 使用自定义安全服务提供的多层加密 / Use multi-layer encryption provided by custom security service
                byte[] protectedData = _customSecurityService.ProtectCriticalData(data, password);
                
                // 模拟异步操作，确保不会阻塞UI线程 / Simulate async operation to ensure UI thread is not blocked
                await Task.Delay(1);
                
                return protectedData;
            }
            catch (Exception ex)
            {
                var securityException = _customSecurityService.ObfuscateException(ex, "加密数据时出错 / Error encrypting data");
                _logger.LogError(securityException, "加密数据时出错 / Error encrypting data");
                throw securityException;
            }
        }

        /// <summary>
        /// 解密数据
        /// Decrypt data
        /// </summary>
        /// <param name="encryptedData">加密的数据 / Encrypted data</param>
        /// <param name="password">密码 / Password</param>
        /// <returns>解密后的数据 / Decrypted data</returns>
        public async Task<byte[]> DecryptAsync(byte[] encryptedData, string password)
        {
            try
            {
                // 验证运行时安全环境 / Verify runtime security environment
                if (!_customSecurityService.VerifyRuntimeSecurity())
                {
                    _logger.LogWarning("检测到不安全的运行环境，解密操作可能不安全 / Detected insecure runtime environment, decryption operation may not be secure");
                }

                // 使用自定义安全服务提供的多层解密 / Use multi-layer decryption provided by custom security service
                byte[] decryptedData = _customSecurityService.UnprotectCriticalData(encryptedData, password);
                
                // 模拟异步操作，确保不会阻塞UI线程 / Simulate async operation to ensure UI thread is not blocked
                await Task.Delay(1);
                
                return decryptedData;
            }
            catch (Exception ex)
            {
                var securityException = _customSecurityService.ObfuscateException(ex, "解密数据时出错 / Error decrypting data");
                _logger.LogError(securityException, "解密数据时出错 / Error decrypting data");
                throw securityException;
            }
        }

        /// <summary>
        /// 验证密码强度
        /// Validate password strength
        /// </summary>
        /// <param name="password">密码 / Password</param>
        /// <returns>验证结果：(是否有效，错误消息) / Validation result: (is valid, error message)</returns>
        public (bool IsValid, string ErrorMessage) ValidatePasswordStrength(string password)
        {
            try
            {
                // 检查密码长度 / Check password length
                if (string.IsNullOrEmpty(password) || password.Length < _minPasswordLength)
                {
                    return (false, $"密码长度必须至少为{_minPasswordLength}个字符 / Password must be at least {_minPasswordLength} characters long");
                }

                // 检查是否包含大写字母 / Check if contains uppercase letter
                if (!password.Any(char.IsUpper))
                {
                    return (false, "密码必须包含至少一个大写字母 / Password must contain at least one uppercase letter");
                }

                // 检查是否包含小写字母 / Check if contains lowercase letter
                if (!password.Any(char.IsLower))
                {
                    return (false, "密码必须包含至少一个小写字母 / Password must contain at least one lowercase letter");
                }

                // 检查是否包含数字 / Check if contains digit
                if (!password.Any(char.IsDigit))
                {
                    return (false, "密码必须包含至少一个数字 / Password must contain at least one digit");
                }

                // 检查是否包含特殊字符 / Check if contains special character
                if (!password.Any(c => !char.IsLetterOrDigit(c)))
                {
                    return (false, "密码必须包含至少一个特殊字符 / Password must contain at least one special character");
                }

                // 检查是否包含常见密码模式 / Check if contains common password patterns
                if (ContainsCommonPasswordPattern(password))
                {
                    return (false, "密码不能包含常见的密码模式 / Password cannot contain common password patterns");
                }

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证密码强度时出错 / Error validating password strength");
                return (false, "无法验证密码强度 / Unable to validate password strength");
            }
        }

        /// <summary>
        /// 生成安全的随机数
        /// Generate secure random number
        /// </summary>
        /// <param name="length">长度 / Length</param>
        /// <returns>随机字节数组 / Random byte array</returns>
        public byte[] GenerateSecureRandomBytes(int length)
        {
            try
            {
                byte[] randomBytes = new byte[length];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(randomBytes);
                return randomBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成安全随机数时出错 / Error generating secure random bytes");
                throw new InvalidOperationException("无法生成安全随机数 / Unable to generate secure random bytes", ex);
            }
        }

        /// <summary>
        /// 检查是否包含常见密码模式
        /// Check if contains common password patterns
        /// </summary>
        /// <param name="password">密码 / Password</param>
        /// <returns>是否包含常见密码模式 / Whether contains common password patterns</returns>
        private bool ContainsCommonPasswordPattern(string password)
        {
            // 检查连续数字 / Check consecutive digits
            if (Regex.IsMatch(password, @"(?:\d{4,})"))
            {
                return true;
            }

            // 检查连续字母 / Check consecutive letters
            if (Regex.IsMatch(password.ToLower(), @"(?:abcd|bcde|cdef|defg|efgh|fghi|ghij|hijk|ijkl|jklm|klmn|lmno|mnop|nopq|opqr|pqrs|qrst|rstu|stuv|tuvw|uvwx|vwxy|wxyz)"))
            {
                return true;
            }

            // 检查键盘模式 / Check keyboard patterns
            string[] keyboardPatterns = new[]
            {
                "qwert", "werty", "ertyu", "rtyui", "tyuio", "yuiop",
                "asdfg", "sdfgh", "dfghj", "fghjk", "ghjkl",
                "zxcvb", "xcvbn", "cvbnm"
            };

            if (keyboardPatterns.Any(pattern => password.ToLower().Contains(pattern)))
            {
                return true;
            }

            // 检查常见单词 / Check common words
            string[] commonWords = new[]
            {
                "password", "welcome", "admin", "administrator", "root",
                "123456", "qwerty", "letmein", "monkey", "abc123"
            };

            if (commonWords.Any(word => password.ToLower().Contains(word)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 生成安全的随机密码
        /// Generate secure random password
        /// </summary>
        public string GenerateSecurePassword(int length = 16)
        {
            const string uppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercaseChars = "abcdefghijklmnopqrstuvwxyz";
            const string digitChars = "0123456789";
            const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

            var password = new StringBuilder();
            using var rng = RandomNumberGenerator.Create();

            // 确保至少包含每种字符
            // Ensure at least one of each character type
            password.Append(GetRandomChar(uppercaseChars, rng));
            password.Append(GetRandomChar(lowercaseChars, rng));
            password.Append(GetRandomChar(digitChars, rng));
            password.Append(GetRandomChar(specialChars, rng));

            // 填充剩余长度
            // Fill remaining length
            string allChars = uppercaseChars + lowercaseChars + digitChars + specialChars;
            for (int i = 4; i < length; i++)
            {
                password.Append(GetRandomChar(allChars, rng));
            }

            // 打乱密码
            // Shuffle password
            return new string(password.ToString().ToCharArray().OrderBy(x => GetRandomInt(rng)).ToArray());
        }

        /// <summary>
        /// 创建密码哈希
        /// Create password hash
        /// </summary>
        public async Task<string> CreatePasswordHashAsync(string password)
        {
            byte[] salt = new byte[SALT_SIZE];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                PBKDF2_ITERATIONS,
                HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(KEY_SIZE);

            byte[] hashBytes = new byte[SALT_SIZE + KEY_SIZE];
            Array.Copy(salt, 0, hashBytes, 0, SALT_SIZE);
            Array.Copy(hash, 0, hashBytes, SALT_SIZE, KEY_SIZE);

            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// 验证密码哈希
        /// Verify password hash
        /// </summary>
        public async Task<bool> VerifyPasswordHashAsync(string password, string storedHash)
        {
            byte[] hashBytes = Convert.FromBase64String(storedHash);
            byte[] salt = new byte[SALT_SIZE];
            Array.Copy(hashBytes, 0, salt, 0, SALT_SIZE);

            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                PBKDF2_ITERATIONS,
                HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(KEY_SIZE);

            for (int i = 0; i < KEY_SIZE; i++)
            {
                if (hashBytes[i + SALT_SIZE] != hash[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 生成会话令牌
        /// Generate session token
        /// </summary>
        public string GenerateSessionToken()
        {
            byte[] tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            return Convert.ToBase64String(tokenBytes);
        }

        private char GetRandomChar(string chars, RandomNumberGenerator rng)
        {
            return chars[GetRandomInt(rng) % chars.Length];
        }

        private int GetRandomInt(RandomNumberGenerator rng)
        {
            byte[] intBytes = new byte[4];
            rng.GetBytes(intBytes);
            return BitConverter.ToInt32(intBytes, 0) & int.MaxValue;
        }
    }
}
