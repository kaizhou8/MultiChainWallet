namespace MultiChainWallet.Core.Models
{
    /// <summary>
    /// 安全设置模型
    /// Security settings model
    /// </summary>
    public class SecuritySettings
    {
        /// <summary>
        /// 用户电子邮件
        /// User email
        /// </summary>
        public string UserEmail { get; set; }

        /// <summary>
        /// 是否启用生物识别
        /// Whether biometric is enabled
        /// </summary>
        public bool IsBiometricEnabled { get; set; }

        /// <summary>
        /// 生物识别设备ID
        /// Biometric device ID
        /// </summary>
        public string BiometricDeviceId { get; set; }

        /// <summary>
        /// 是否启用TOTP
        /// Whether TOTP is enabled
        /// </summary>
        public bool IsTotpEnabled { get; set; }

        /// <summary>
        /// TOTP密钥
        /// TOTP secret key
        /// </summary>
        public string TotpSecretKey { get; set; }

        /// <summary>
        /// 是否启用自动锁定
        /// Whether auto-lock is enabled
        /// </summary>
        public bool IsAutoLockEnabled { get; set; }

        /// <summary>
        /// 自动锁定超时（分钟）
        /// Auto-lock timeout (minutes)
        /// </summary>
        public int AutoLockTimeout { get; set; } = 5;

        /// <summary>
        /// 是否启用交易确认
        /// Whether transaction confirmation is enabled
        /// </summary>
        public bool IsTransactionConfirmationEnabled { get; set; } = true;

        /// <summary>
        /// 最后更新时间
        /// Last update time
        /// </summary>
        public System.DateTime LastUpdated { get; set; }
    }
} 