using System.Threading.Tasks;

namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// 多因素认证类型
    /// Multi-factor authentication type
    /// </summary>
    public enum MfaType
    {
        /// <summary>
        /// 生物识别
        /// Biometric
        /// </summary>
        Biometric,

        /// <summary>
        /// 基于时间的一次性密码
        /// Time-based One-Time Password
        /// </summary>
        Totp
    }

    /// <summary>
    /// MFA设置结果
    /// MFA setup result
    /// </summary>
    public class MfaSetupResult
    {
        /// <summary>
        /// 是否成功
        /// Whether successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误消息
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 额外数据（如TOTP密钥）
        /// Additional data (such as TOTP secret)
        /// </summary>
        public object AdditionalData { get; set; }
    }

    /// <summary>
    /// 多因素认证管理器接口
    /// Multi-factor authentication manager interface
    /// </summary>
    public interface IMultiFactorAuthManager
    {
        /// <summary>
        /// 检查MFA设置是否完成
        /// Check if MFA setup is complete
        /// </summary>
        Task<bool> IsSetupCompleteAsync();

        /// <summary>
        /// 设置MFA
        /// Setup MFA
        /// </summary>
        /// <param name="type">MFA类型 / MFA type</param>
        /// <param name="password">用户密码 / User password</param>
        Task<MfaSetupResult> SetupMfaAsync(MfaType type, string password);

        /// <summary>
        /// 验证MFA
        /// Verify MFA
        /// </summary>
        /// <param name="type">MFA类型 / MFA type</param>
        /// <param name="code">验证码（用于TOTP）/ Verification code (for TOTP)</param>
        Task<bool> VerifyMfaAsync(MfaType type, string code = null);

        /// <summary>
        /// 禁用MFA
        /// Disable MFA
        /// </summary>
        /// <param name="type">MFA类型 / MFA type</param>
        /// <param name="password">用户密码 / User password</param>
        Task<bool> DisableMfaAsync(MfaType type, string password);

        /// <summary>
        /// 获取MFA状态
        /// Get MFA status
        /// </summary>
        Task<(bool IsBiometricEnabled, bool IsTotpEnabled)> GetMfaStatusAsync();
    }
} 