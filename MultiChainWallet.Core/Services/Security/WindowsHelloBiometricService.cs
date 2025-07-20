using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Interfaces;
using Windows.Security.Credentials;

namespace MultiChainWallet.Core.Services.Security
{
    /// <summary>
    /// Windows Hello 生物识别服务实现
    /// Windows Hello biometric service implementation
    /// </summary>
    public class WindowsHelloBiometricService : IBiometricAuthService
    {
        private readonly ILogger<WindowsHelloBiometricService> _logger;
        private readonly ISecurityService _securityService;

        public WindowsHelloBiometricService(
            ILogger<WindowsHelloBiometricService> logger,
            ISecurityService securityService)
        {
            _logger = logger;
            _securityService = securityService;
        }

        /// <inheritdoc/>
        public async Task<bool> IsBiometricAvailableAsync()
        {
            try
            {
                var availabilityResult = await UserConsentVerifier.CheckAvailabilityAsync();
                return availabilityResult == UserConsentVerifierAvailability.Available;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查生物识别可用性时出错 / Error checking biometric availability");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> AuthenticateAsync(string message)
        {
            try
            {
                if (!await IsBiometricAvailableAsync())
                {
                    _logger.LogWarning("生物识别不可用 / Biometric authentication not available");
                    return false;
                }

                var consentResult = await UserConsentVerifier.RequestVerificationAsync(message);
                bool isVerified = consentResult == UserConsentVerifierResult.Verified;

                if (!isVerified)
                {
                    _logger.LogWarning("生物识别验证失败: {Result} / Biometric verification failed: {Result}", consentResult);
                }

                return isVerified;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生物识别认证过程中出错 / Error during biometric authentication");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string DeviceId)> RegisterDeviceAsync()
        {
            try
            {
                if (!await IsBiometricAvailableAsync())
                {
                    _logger.LogWarning("无法注册设备：生物识别不可用 / Cannot register device: Biometric not available");
                    return (false, string.Empty);
                }

                // 生成设备ID / Generate device ID
                string deviceId = _securityService.GenerateSessionToken();

                // 请求用户进行生物识别验证以关联设备
                // Request user to perform biometric verification to associate device
                var consentResult = await UserConsentVerifier.RequestVerificationAsync(
                    "请使用生物识别验证以注册此设备 / Please use biometric verification to register this device");

                if (consentResult == UserConsentVerifierResult.Verified)
                {
                    _logger.LogInformation("设备注册成功 / Device registration successful");
                    return (true, deviceId);
                }

                _logger.LogWarning("设备注册失败：用户验证失败 / Device registration failed: User verification failed");
                return (false, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注册设备时出错 / Error registering device");
                return (false, string.Empty);
            }
        }
    }
} 