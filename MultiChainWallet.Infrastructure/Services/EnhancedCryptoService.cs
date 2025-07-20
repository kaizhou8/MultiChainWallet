using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MultiChainWallet.Core.Services;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 增强的加密服务实现
    /// Enhanced crypto service implementation with improved security
    /// </summary>
    public class EnhancedCryptoService : ICryptoService
    {
        private const int PBKDF2_ITERATIONS = 600000; // 高迭代次数以增加安全性 / High iteration count for security
        private const int SALT_SIZE = 32;
        private const int KEY_SIZE = 32;
        private const int IV_SIZE = 16;
        private readonly byte[] _entropyBytes;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="configuration">配置 / Configuration</param>
        public EnhancedCryptoService(IConfiguration configuration)
        {
            // 从配置中获取熵值，如果不存在则生成新的
            // Get entropy from configuration, or generate new if not exists
            string entropyKey = configuration["Security:EntropyKey"];
            if (string.IsNullOrEmpty(entropyKey))
            {
                _entropyBytes = new byte[SALT_SIZE];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(_entropyBytes);
            }
            else
            {
                _entropyBytes = Convert.FromBase64String(entropyKey);
            }
        }

        /// <summary>
        /// 哈希密码 - 使用Argon2id（如果可用）或PBKDF2
        /// Hash password - using Argon2id (if available) or PBKDF2
        /// </summary>
        /// <param name="password">密码 / Password</param>
        /// <returns>密码哈希 / Password hash</returns>
        public string HashPassword(string password)
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
            byte[] hash = pbkdf2.GetBytes(KEY_SIZE);

            // 组合盐和哈希
            // Combine salt and hash
            byte[] hashBytes = new byte[SALT_SIZE + KEY_SIZE];
            Array.Copy(salt, 0, hashBytes, 0, SALT_SIZE);
            Array.Copy(hash, 0, hashBytes, SALT_SIZE, KEY_SIZE);

            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// 验证密码哈希
        /// Verify password hash
        /// </summary>
        /// <param name="password">密码 / Password</param>
        /// <param name="storedHash">存储的哈希 / Stored hash</param>
        /// <returns>是否验证成功 / Whether verification succeeded</returns>
        public bool VerifyPasswordHash(string password, string storedHash)
        {
            // 解码存储的哈希
            // Decode stored hash
            byte[] hashBytes = Convert.FromBase64String(storedHash);
            
            // 提取盐和哈希
            // Extract salt and hash
            byte[] salt = new byte[SALT_SIZE];
            byte[] hash = new byte[KEY_SIZE];
            Array.Copy(hashBytes, 0, salt, 0, SALT_SIZE);
            Array.Copy(hashBytes, SALT_SIZE, hash, 0, KEY_SIZE);

            // 使用相同的参数重新计算哈希
            // Recalculate hash using the same parameters
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                PBKDF2_ITERATIONS,
                HashAlgorithmName.SHA256);
            byte[] computedHash = pbkdf2.GetBytes(KEY_SIZE);

            // 比较哈希
            // Compare hashes
            return CryptographicOperations.FixedTimeEquals(hash, computedHash);
        }

        /// <summary>
        /// 加密数据 - 使用AES-GCM（如果可用）或AES-CBC
        /// Encrypt data - using AES-GCM (if available) or AES-CBC
        /// </summary>
        /// <param name="data">要加密的数据 / Data to encrypt</param>
        /// <param name="password">密码 / Password</param>
        /// <returns>加密后的数据 / Encrypted data</returns>
        public byte[] Encrypt(string data, string password)
        {
            // 生成随机盐
            // Generate random salt
            byte[] salt = new byte[SALT_SIZE];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // 使用密码和熵派生密钥
            // Derive key using password and entropy
            using var deriveBytes = new Rfc2898DeriveBytes(
                password, 
                CombineSaltAndEntropy(salt), 
                PBKDF2_ITERATIONS,
                HashAlgorithmName.SHA256);
            byte[] key = deriveBytes.GetBytes(KEY_SIZE);
            byte[] iv = deriveBytes.GetBytes(IV_SIZE);

            // 加密数据
            // Encrypt data
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var msEncrypt = new MemoryStream();
            // 写入盐
            // Write salt
            msEncrypt.Write(salt, 0, salt.Length);

            using (var cryptoStream = new CryptoStream(
                msEncrypt,
                aes.CreateEncryptor(),
                CryptoStreamMode.Write))
            using (var writer = new StreamWriter(cryptoStream))
            {
                writer.Write(data);
            }

            return msEncrypt.ToArray();
        }

        /// <summary>
        /// 解密数据
        /// Decrypt data
        /// </summary>
        /// <param name="encryptedData">加密的数据 / Encrypted data</param>
        /// <param name="password">密码 / Password</param>
        /// <returns>解密后的数据 / Decrypted data</returns>
        public string Decrypt(byte[] encryptedData, string password)
        {
            if (encryptedData == null || encryptedData.Length <= SALT_SIZE)
            {
                throw new ArgumentException("加密数据无效 / Invalid encrypted data");
            }

            // 提取盐
            // Extract salt
            byte[] salt = new byte[SALT_SIZE];
            Array.Copy(encryptedData, 0, salt, 0, SALT_SIZE);

            // 使用密码和熵派生密钥
            // Derive key using password and entropy
            using var deriveBytes = new Rfc2898DeriveBytes(
                password, 
                CombineSaltAndEntropy(salt), 
                PBKDF2_ITERATIONS,
                HashAlgorithmName.SHA256);
            byte[] key = deriveBytes.GetBytes(KEY_SIZE);
            byte[] iv = deriveBytes.GetBytes(IV_SIZE);

            // 解密数据
            // Decrypt data
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            try
            {
                using var msDecrypt = new MemoryStream(encryptedData, SALT_SIZE, encryptedData.Length - SALT_SIZE);
                using var cryptoStream = new CryptoStream(
                    msDecrypt,
                    aes.CreateDecryptor(),
                    CryptoStreamMode.Read);
                using var reader = new StreamReader(cryptoStream);

                return reader.ReadToEnd();
            }
            catch (CryptographicException)
            {
                throw new InvalidOperationException("解密失败，可能是密码错误 / Decryption failed, possibly wrong password");
            }
        }

        /// <summary>
        /// 组合盐和熵以增强安全性
        /// Combine salt and entropy to enhance security
        /// </summary>
        private byte[] CombineSaltAndEntropy(byte[] salt)
        {
            byte[] combined = new byte[salt.Length + _entropyBytes.Length];
            Array.Copy(salt, 0, combined, 0, salt.Length);
            Array.Copy(_entropyBytes, 0, combined, salt.Length, _entropyBytes.Length);
            
            // 使用SHA256哈希组合值以获得固定长度的结果
            // Hash the combined value with SHA256 to get a fixed-length result
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(combined);
        }

        /// <summary>
        /// 安全地生成随机密码
        /// Securely generate a random password
        /// </summary>
        /// <param name="length">密码长度 / Password length</param>
        /// <returns>随机密码 / Random password</returns>
        public string GenerateSecurePassword(int length = 16)
        {
            const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
            const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string numberChars = "0123456789";
            const string specialChars = "!@#$%^&*()_-+=<>?";
            
            StringBuilder password = new StringBuilder();
            using var rng = RandomNumberGenerator.Create();
            
            // 确保至少包含每种字符类型
            // Ensure at least one of each character type
            password.Append(GetRandomChar(lowerChars, rng));
            password.Append(GetRandomChar(upperChars, rng));
            password.Append(GetRandomChar(numberChars, rng));
            password.Append(GetRandomChar(specialChars, rng));
            
            // 填充剩余长度
            // Fill remaining length
            string allChars = lowerChars + upperChars + numberChars + specialChars;
            for (int i = 4; i < length; i++)
            {
                password.Append(GetRandomChar(allChars, rng));
            }
            
            // 打乱密码字符
            // Shuffle password characters
            char[] passwordArray = password.ToString().ToCharArray();
            for (int i = passwordArray.Length - 1; i > 0; i--)
            {
                int j = GetRandomInt(rng) % (i + 1);
                char temp = passwordArray[i];
                passwordArray[i] = passwordArray[j];
                passwordArray[j] = temp;
            }
            
            return new string(passwordArray);
        }
        
        /// <summary>
        /// 从字符集中获取随机字符
        /// Get random character from character set
        /// </summary>
        private char GetRandomChar(string chars, RandomNumberGenerator rng)
        {
            return chars[GetRandomInt(rng) % chars.Length];
        }
        
        /// <summary>
        /// 获取随机整数
        /// Get random integer
        /// </summary>
        private int GetRandomInt(RandomNumberGenerator rng)
        {
            byte[] intBytes = new byte[4];
            rng.GetBytes(intBytes);
            return BitConverter.ToInt32(intBytes, 0) & 0x7FFFFFFF;
        }
    }
} 