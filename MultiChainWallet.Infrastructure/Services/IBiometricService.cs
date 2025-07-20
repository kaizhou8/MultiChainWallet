using Plugin.Fingerprint.Abstractions;
using System.Threading.Tasks;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 生物识别类型
    /// Biometric type
    /// </summary>
    public enum BiometricType
    {
        /// <summary>
        /// 不支持
        /// Not supported
        /// </summary>
        None,
        
        /// <summary>
        /// 指纹识别
        /// Fingerprint recognition
        /// </summary>
        Fingerprint,
        
        /// <summary>
        /// 面部识别
        /// Facial recognition
        /// </summary>
        Face,
        
        /// <summary>
        /// 虹膜识别
        /// Iris recognition
        /// </summary>
        Iris
    }

    /// <summary>
    /// 生物识别服务接口
    /// Biometric service interface
    /// </summary>
    public interface IBiometricService
    {
        /// <summary>
        /// 检查设备是否支持生物识别
        /// Check if the device supports biometric authentication
        /// </summary>
        /// <returns>设备是否支持生物识别</returns>
        Task<bool> IsBiometricSupportedAsync();

        /// <summary>
        /// 获取设备支持的生物识别类型
        /// Get the biometric authentication type supported by the device
        /// </summary>
        /// <returns>生物识别类型</returns>
        Task<BiometricType> GetBiometricTypeAsync();

        /// <summary>
        /// 验证用户身份
        /// Authenticate the user
        /// </summary>
        /// <param name="title">验证对话框标题</param>
        /// <param name="description">验证描述</param>
        /// <returns>验证结果</returns>
        Task<FingerprintAuthenticationResult> AuthenticateAsync(string title, string description);

        /// <summary>
        /// 验证用户身份用于交易确认
        /// Authenticate the user for transaction confirmation
        /// </summary>
        /// <param name="transactionInfo">交易信息描述</param>
        /// <returns>验证结果</returns>
        Task<FingerprintAuthenticationResult> AuthenticateForTransactionAsync(string transactionInfo);

        /// <summary>
        /// 验证用户身份用于钱包解锁
        /// Authenticate the user for wallet unlocking
        /// </summary>
        /// <returns>验证结果</returns>
        Task<FingerprintAuthenticationResult> AuthenticateForWalletUnlockAsync();
    }
} 