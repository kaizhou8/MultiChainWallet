using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Interfaces;

namespace MultiChainWallet.Core.Services.Security
{
    /// <summary>
    /// 交易确认服务实现
    /// Transaction confirmation service implementation
    /// </summary>
    public class TransactionConfirmationService : ITransactionConfirmationService
    {
        private readonly IUserSettingsService _userSettingsService;
        private readonly IMultiFactorAuthManager _mfaManager;
        private readonly IBiometricAuthService _biometricAuthService;
        private readonly ILogger<TransactionConfirmationService> _logger;

        public TransactionConfirmationService(
            IUserSettingsService userSettingsService,
            IMultiFactorAuthManager mfaManager,
            IBiometricAuthService biometricAuthService,
            ILogger<TransactionConfirmationService> logger)
        {
            _userSettingsService = userSettingsService;
            _mfaManager = mfaManager;
            _biometricAuthService = biometricAuthService;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<bool> RequestConfirmationAsync(string fromAddress, string toAddress, string amount, string currency, string fee)
        {
            try
            {
                // 检查是否需要确认 / Check if confirmation is required
                if (!await IsConfirmationRequiredAsync())
                {
                    return true;
                }

                // 在Core层，我们只返回false，让UI层处理具体的确认逻辑
                // In Core layer, we just return false and let UI layer handle the confirmation logic
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "请求交易确认时出错 / Error requesting transaction confirmation");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsConfirmationRequiredAsync()
        {
            try
            {
                var settings = await _userSettingsService.GetSecuritySettingsAsync();
                return settings.IsTransactionConfirmationEnabled;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查是否需要交易确认时出错 / Error checking if transaction confirmation is required");
                return true; // 出错时默认需要确认 / Default to requiring confirmation when error occurs
            }
        }

        /// <inheritdoc/>
        public async Task<(bool IsBiometricAvailable, bool IsTotpEnabled)> GetAvailableAuthMethodsAsync()
        {
            try
            {
                var isBiometricAvailable = await _biometricAuthService.IsBiometricAvailableAsync();
                var mfaStatus = await _mfaManager.GetMfaStatusAsync();

                return (isBiometricAvailable, mfaStatus.IsTotpEnabled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取可用认证方式时出错 / Error getting available authentication methods");
                return (false, false);
            }
        }
    }
} 