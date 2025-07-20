using System.Threading.Tasks;

namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// 安全服务接口 / Security service interface
    /// </summary>
    public interface ISecurityService
    {
        /// <summary>
        /// 加密数据 / Encrypt data
        /// </summary>
        Task<byte[]> EncryptAsync(byte[] data, string password);

        /// <summary>
        /// 解密数据 / Decrypt data
        /// </summary>
        Task<byte[]> DecryptAsync(byte[] encryptedData, string password);

        /// <summary>
        /// 验证密码强度
        /// Validate password strength
        /// </summary>
        /// <param name="password">密码 / Password</param>
        /// <returns>验证结果：(是否有效，错误消息) / Validation result: (is valid, error message)</returns>
        (bool IsValid, string ErrorMessage) ValidatePasswordStrength(string password);

        /// <summary>
        /// 生成安全的随机数 / Generate secure random number
        /// </summary>
        byte[] GenerateSecureRandomBytes(int length);

        /// <summary>
        /// 生成会话令牌 / Generate session token
        /// </summary>
        /// <returns>会话令牌 / Session token</returns>
        Task<string> GenerateSessionTokenAsync();

        /// <summary>
        /// 验证密码哈希 / Verify password hash
        /// </summary>
        /// <param name="password">密码 / Password</param>
        /// <param name="storedHash">存储的哈希值 / Stored hash</param>
        /// <returns>验证结果 / Verification result</returns>
        Task<bool> VerifyPasswordHashAsync(string password, string storedHash);
    }
}
