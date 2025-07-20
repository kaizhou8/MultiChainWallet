using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MultiChainWallet.Core.Services;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 加密服务实现
    /// Crypto service implementation
    /// </summary>
    public class CryptoService : ICryptoService
    {
        private readonly string _salt = "MultiChainWallet2025";

        /// <summary>
        /// 哈希密码
        /// Hash password
        /// </summary>
        /// <param name="password">密码 / Password</param>
        /// <returns>密码哈希 / Password hash</returns>
        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var saltedPassword = password + _salt;
            var bytes = Encoding.UTF8.GetBytes(saltedPassword);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// 加密数据
        /// Encrypt data
        /// </summary>
        /// <param name="data">要加密的数据 / Data to encrypt</param>
        /// <param name="password">密码 / Password</param>
        /// <returns>加密后的数据 / Encrypted data</returns>
        public byte[] Encrypt(string data, string password)
        {
            using var aes = Aes.Create();
            var key = DeriveKey(password);
            aes.Key = key;
            aes.GenerateIV();

            using var msEncrypt = new MemoryStream();
            msEncrypt.Write(aes.IV, 0, aes.IV.Length);

            using (var csEncrypt = new CryptoStream(msEncrypt, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(data);
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
            using var aes = Aes.Create();
            var key = DeriveKey(password);
            aes.Key = key;

            var iv = new byte[16];
            Array.Copy(encryptedData, 0, iv, 0, 16);
            aes.IV = iv;

            using var msDecrypt = new MemoryStream(encryptedData, 16, encryptedData.Length - 16);
            using var csDecrypt = new CryptoStream(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            return srDecrypt.ReadToEnd();
        }

        /// <summary>
        /// 从密码派生密钥
        /// Derive key from password
        /// </summary>
        private byte[] DeriveKey(string password)
        {
            using var deriveBytes = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(_salt), 10000);
            return deriveBytes.GetBytes(32); // 256 bits
        }
    }
}
