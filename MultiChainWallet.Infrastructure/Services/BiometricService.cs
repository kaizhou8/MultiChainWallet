using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using System;
using System.Threading.Tasks;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 生物识别服务，提供指纹和面部识别功能
    /// Biometric service providing fingerprint and facial recognition
    /// </summary>
    public class BiometricService : IBiometricService
    {
        private readonly IFingerprint _fingerprint;

        public BiometricService()
        {
            _fingerprint = CrossFingerprint.Current;
        }

        /// <summary>
        /// 检查设备是否支持生物识别
        /// Check if the device supports biometric authentication
        /// </summary>
        /// <returns>设备是否支持生物识别</returns>
        public async Task<bool> IsBiometricSupportedAsync()
        {
            return await _fingerprint.IsAvailableAsync();
        }

        /// <summary>
        /// 获取设备支持的生物识别类型
        /// Get the biometric authentication type supported by the device
        /// </summary>
        /// <returns>生物识别类型</returns>
        public async Task<BiometricType> GetBiometricTypeAsync()
        {
            if (!await IsBiometricSupportedAsync())
                return BiometricType.None;

            var availability = await _fingerprint.GetAvailabilityAsync();
            
            // 根据可用性确定生物识别类型
            // Determine biometric type based on availability
            switch (availability)
            {
                case FingerprintAvailability.Available:
                    return BiometricType.Fingerprint;
                case FingerprintAvailability.Available | FingerprintAvailability.Face:
                    return BiometricType.Face;
                case FingerprintAvailability.Available | FingerprintAvailability.Iris:
                    return BiometricType.Iris;
                default:
                    return BiometricType.None;
            }
        }

        /// <summary>
        /// 验证用户身份
        /// Authenticate the user
        /// </summary>
        /// <param name="title">验证对话框标题</param>
        /// <param name="description">验证描述</param>
        /// <returns>验证结果</returns>
        public async Task<FingerprintAuthenticationResult> AuthenticateAsync(string title, string description)
        {
            var authRequestConfig = new AuthenticationRequestConfiguration(
                title,
                description)
            {
                AllowAlternativeAuthentication = true,
                ConfirmationRequired = true
            };

            return await _fingerprint.AuthenticateAsync(authRequestConfig);
        }

        /// <summary>
        /// 验证用户身份用于交易确认
        /// Authenticate the user for transaction confirmation
        /// </summary>
        /// <param name="transactionInfo">交易信息描述</param>
        /// <returns>验证结果</returns>
        public async Task<FingerprintAuthenticationResult> AuthenticateForTransactionAsync(string transactionInfo)
        {
            var biometricType = await GetBiometricTypeAsync();
            string authMethod = biometricType == BiometricType.Face ? "面部识别" : "生物识别";
            string authMethodEn = biometricType == BiometricType.Face ? "facial recognition" : "biometric authentication";
            
            return await AuthenticateAsync(
                $"交易确认 | Transaction Confirmation",
                $"请使用{authMethod}确认以下交易：\n{transactionInfo}\n\nPlease use {authMethodEn} to confirm the following transaction:\n{transactionInfo}");
        }

        /// <summary>
        /// 验证用户身份用于钱包解锁
        /// Authenticate the user for wallet unlocking
        /// </summary>
        /// <returns>验证结果</returns>
        public async Task<FingerprintAuthenticationResult> AuthenticateForWalletUnlockAsync()
        {
            var biometricType = await GetBiometricTypeAsync();
            string authMethod = biometricType == BiometricType.Face ? "面部识别" : "生物识别";
            string authMethodEn = biometricType == BiometricType.Face ? "facial recognition" : "biometric authentication";
            
            return await AuthenticateAsync(
                $"钱包解锁 | Wallet Unlock",
                $"请使用{authMethod}解锁钱包\nPlease use {authMethodEn} to unlock your wallet");
        }
    }
} 