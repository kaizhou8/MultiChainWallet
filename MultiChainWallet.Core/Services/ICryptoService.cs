namespace MultiChainWallet.Core.Services
{
    /// <summary>
    /// 加密服务接口
    /// Crypto service interface
    /// </summary>
    public interface ICryptoService
    {
        /// <summary>
        /// 哈希密码
        /// Hash password
        /// </summary>
        /// <param name="password">密码 / Password</param>
        /// <returns>密码哈希 / Password hash</returns>
        string HashPassword(string password);

        /// <summary>
        /// 加密数据
        /// Encrypt data
        /// </summary>
        /// <param name="data">要加密的数据 / Data to encrypt</param>
        /// <param name="password">密码 / Password</param>
        /// <returns>加密后的数据 / Encrypted data</returns>
        byte[] Encrypt(string data, string password);

        /// <summary>
        /// 解密数据
        /// Decrypt data
        /// </summary>
        /// <param name="encryptedData">加密的数据 / Encrypted data</param>
        /// <param name="password">密码 / Password</param>
        /// <returns>解密后的数据 / Decrypted data</returns>
        string Decrypt(byte[] encryptedData, string password);
    }
}
