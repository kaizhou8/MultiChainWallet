using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Interfaces;

namespace MultiChainWallet.Core.Services.Security
{
    /// <summary>
    /// 多因素认证管理器实现
    /// Multi-factor authentication manager implementation
    /// </summary>
    public class MultiFactorAuthManager : IMultiFactorAuthManager
    {
        private readonly IBiometricAuthService _biometricAuthService;
        private readonly ITotpService _totpService;
        private readonly ISecurityService _securityService;
        private readonly IUserSettingsService _userSettingsService;
        private readonly ILogger<MultiFactorAuthManager> _logger;

        public MultiFactorAuthManager(
            IBiometricAuthService biometricAuthService,
            ITotpService totpService,
            ISecurityService securityService,
            IUserSettingsService userSettingsService,
            ILogger<MultiFactorAuthManager> logger)
        {
            _biometricAuthService = biometricAuthService;
            _totpService = totpService;
            _securityService = securityService;
            _userSettingsService = userSettingsService;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<bool> IsSetupCompleteAsync()
        {
            try
            {
                var settings = await _userSettingsService.GetSecuritySettingsAsync();
                return settings.IsBiometricEnabled || settings.IsTotpEnabled;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查MFA设置状态时出错 / Error checking MFA setup status");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<MfaSetupResult> SetupMfaAsync(MfaType type, string password)
        {
            try
            {
                // 验证密码
                // Verify password
                if (!await _securityService.VerifyPasswordHashAsync(password, 
                    await _userSettingsService.GetPasswordHashAsync()))
                {
                    return new MfaSetupResult 
                    { 
                        Success = false, 
                        ErrorMessage = "密码不正确 / Password is incorrect" 
                    };
                }

                var settings = await _userSettingsService.GetSecuritySettingsAsync();

                switch (type)
                {
                    case MfaType.Biometric:
                        if (!await _biometricAuthService.IsBiometricAvailableAsync())
                        {
                            return new MfaSetupResult 
                            { 
                                Success = false, 
                                ErrorMessage = "此设备不支持生物识别 / Biometrics not supported on this device" 
                            };
                        }

                        var (success, deviceId) = await _biometricAuthService.RegisterDeviceAsync();
                        if (success)
                        {
                            settings.IsBiometricEnabled = true;
                            settings.BiometricDeviceId = deviceId;
                            await _userSettingsService.SaveSecuritySettingsAsync(settings);
                            return new MfaSetupResult { Success = true };
                        }
                        return new MfaSetupResult 
                        { 
                            Success = false, 
                            ErrorMessage = "生物识别注册失败 / Biometric registration failed" 
                        };

                    case MfaType.Totp:
                        string secretKey = await _totpService.GenerateSecretKeyAsync();
                        string qrCodeUri = _totpService.GenerateQrCodeUri(
                            secretKey, 
                            settings.UserEmail, 
                            "MultiChainWallet");

                        settings.IsTotpEnabled = true;
                        settings.TotpSecretKey = secretKey;
                        await _userSettingsService.SaveSecuritySettingsAsync(settings);

                        return new MfaSetupResult 
                        { 
                            Success = true,
                            AdditionalData = new { SecretKey = secretKey, QrCodeUri = qrCodeUri }
                        };

                    default:
                        return new MfaSetupResult 
                        { 
                            Success = false, 
                            ErrorMessage = "不支持的MFA类型 / Unsupported MFA type" 
                        };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置MFA时出错 / Error setting up MFA");
                return new MfaSetupResult 
                { 
                    Success = false, 
                    ErrorMessage = "设置MFA时发生错误 / Error occurred while setting up MFA" 
                };
            }
        }

        /// <inheritdoc/>
        public async Task<bool> VerifyMfaAsync(MfaType type, string code = null)
        {
            try
            {
                var settings = await _userSettingsService.GetSecuritySettingsAsync();

                switch (type)
                {
                    case MfaType.Biometric:
                        if (!settings.IsBiometricEnabled)
                        {
                            _logger.LogWarning("尝试验证未启用的生物识别 / Attempting to verify disabled biometric");
                            return false;
                        }
                        return await _biometricAuthService.AuthenticateAsync(
                            "请使用生物识别进行验证 / Please verify using biometrics");

                    case MfaType.Totp:
                        if (!settings.IsTotpEnabled)
                        {
                            _logger.LogWarning("尝试验证未启用的TOTP / Attempting to verify disabled TOTP");
                            return false;
                        }
                        if (string.IsNullOrEmpty(code))
                        {
                            _logger.LogWarning("TOTP验证码为空 / TOTP code is empty");
                            return false;
                        }
                        return _totpService.VerifyCode(settings.TotpSecretKey, code);

                    default:
                        _logger.LogWarning("不支持的MFA类型: {Type} / Unsupported MFA type: {Type}", type);
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证MFA时出错 / Error verifying MFA");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DisableMfaAsync(MfaType type, string password)
        {
            try
            {
                // 验证密码
                // Verify password
                if (!await _securityService.VerifyPasswordHashAsync(password, 
                    await _userSettingsService.GetPasswordHashAsync()))
                {
                    _logger.LogWarning("禁用MFA时密码验证失败 / Password verification failed while disabling MFA");
                    return false;
                }

                var settings = await _userSettingsService.GetSecuritySettingsAsync();

                switch (type)
                {
                    case MfaType.Biometric:
                        settings.IsBiometricEnabled = false;
                        settings.BiometricDeviceId = null;
                        break;

                    case MfaType.Totp:
                        settings.IsTotpEnabled = false;
                        settings.TotpSecretKey = null;
                        break;

                    default:
                        _logger.LogWarning("尝试禁用不支持的MFA类型: {Type} / Attempting to disable unsupported MFA type: {Type}", type);
                        return false;
                }

                await _userSettingsService.SaveSecuritySettingsAsync(settings);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "禁用MFA时出错 / Error disabling MFA");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<(bool IsBiometricEnabled, bool IsTotpEnabled)> GetMfaStatusAsync()
        {
            try
            {
                var settings = await _userSettingsService.GetSecuritySettingsAsync();
                return (settings.IsBiometricEnabled, settings.IsTotpEnabled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取MFA状态时出错 / Error getting MFA status");
                return (false, false);
            }
        }
    }
} 