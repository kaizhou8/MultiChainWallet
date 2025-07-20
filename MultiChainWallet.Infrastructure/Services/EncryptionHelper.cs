using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 加密帮助类
    /// Encryption helper class
    /// </summary>
    public static class EncryptionHelper
    {
        private const int KeySize = 256;
        private const int BlockSize = 128;
        private const int Iterations = 10000;

        /// <summary>
        /// 加密数据
        /// Encrypt data
        /// </summary>
        public static byte[] Encrypt(string plainText, string password)
        {
            // 生成随机盐
            // Generate random salt
            byte[] salt = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // 生成密钥和IV
            // Generate key and IV
            using var keyDerivation = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] key = keyDerivation.GetBytes(KeySize / 8);
            byte[] iv = keyDerivation.GetBytes(BlockSize / 8);

            // 加密数据
            // Encrypt data
            using var aes = Aes.Create();
            aes.KeySize = KeySize;
            aes.BlockSize = BlockSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var memoryStream = new MemoryStream();
            // 写入盐
            // Write salt
            memoryStream.Write(salt, 0, salt.Length);

            using (var cryptoStream = new CryptoStream(
                memoryStream,
                aes.CreateEncryptor(key, iv),
                CryptoStreamMode.Write))
            {
                using var writer = new StreamWriter(cryptoStream);
                writer.Write(plainText);
            }

            return memoryStream.ToArray();
        }

        /// <summary>
        /// 解密数据
        /// Decrypt data
        /// </summary>
        public static string Decrypt(byte[] encryptedData, string password)
        {
            // 读取盐
            // Read salt
            byte[] salt = new byte[32];
            Array.Copy(encryptedData, 0, salt, 0, salt.Length);

            // 生成密钥和IV
            // Generate key and IV
            using var keyDerivation = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] key = keyDerivation.GetBytes(KeySize / 8);
            byte[] iv = keyDerivation.GetBytes(BlockSize / 8);

            // 解密数据
            // Decrypt data
            using var aes = Aes.Create();
            aes.KeySize = KeySize;
            aes.BlockSize = BlockSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var memoryStream = new MemoryStream(encryptedData, salt.Length, encryptedData.Length - salt.Length);
            using var cryptoStream = new CryptoStream(
                memoryStream,
                aes.CreateDecryptor(key, iv),
                CryptoStreamMode.Read);
            using var reader = new StreamReader(cryptoStream);

            return reader.ReadToEnd();
        }
    }
}
