using System.Threading.Tasks;

namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// TOTP (基于时间的一次性密码) 服务接口
    /// TOTP (Time-based One-Time Password) service interface
    /// </summary>
    public interface ITotpService
    {
        /// <summary>
        /// 生成新的密钥
        /// Generate new secret key
        /// </summary>
        /// <returns>Base32编码的密钥 / Base32 encoded secret key</returns>
        Task<string> GenerateSecretKeyAsync();

        /// <summary>
        /// 生成用于QR码的URI
        /// Generate URI for QR code
        /// </summary>
        /// <param name="secretKey">密钥 / Secret key</param>
        /// <param name="accountName">账户名称 / Account name</param>
        /// <param name="issuer">发行者 / Issuer</param>
        /// <returns>otpauth URI</returns>
        string GenerateQrCodeUri(string secretKey, string accountName, string issuer);

        /// <summary>
        /// 验证TOTP代码
        /// Verify TOTP code
        /// </summary>
        /// <param name="secretKey">密钥 / Secret key</param>
        /// <param name="code">要验证的代码 / Code to verify</param>
        /// <returns>如果代码有效则返回true / Returns true if code is valid</returns>
        bool VerifyCode(string secretKey, string code);

        /// <summary>
        /// 获取当前TOTP代码
        /// Get current TOTP code
        /// </summary>
        /// <param name="secretKey">密钥 / Secret key</param>
        /// <returns>当前的TOTP代码 / Current TOTP code</returns>
        string GetCurrentCode(string secretKey);
    }
} 