using System.Threading.Tasks;

namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// 生物识别认证服务接口
    /// Biometric authentication service interface
    /// </summary>
    public interface IBiometricAuthService
    {
        /// <summary>
        /// 检查生物识别是否可用
        /// Check if biometric authentication is available
        /// </summary>
        /// <returns>如果生物识别可用则返回true / Returns true if biometric authentication is available</returns>
        Task<bool> IsBiometricAvailableAsync();

        /// <summary>
        /// 执行生物识别认证
        /// Perform biometric authentication
        /// </summary>
        /// <param name="message">显示给用户的提示消息 / Message to display to user</param>
        /// <returns>如果认证成功则返回true / Returns true if authentication is successful</returns>
        Task<bool> AuthenticateAsync(string message);

        /// <summary>
        /// 注册设备用于生物识别
        /// Register device for biometric authentication
        /// </summary>
        /// <returns>
        /// (成功状态, 设备ID)的元组
        /// Tuple of (success status, device ID)
        /// </returns>
        Task<(bool Success, string DeviceId)> RegisterDeviceAsync();
    }
} 