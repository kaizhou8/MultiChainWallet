using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Interfaces;
using MultiChainWallet.UI.Pages;
using MultiChainWallet.UI.ViewModels;

namespace MultiChainWallet.UI.Services
{
    /// <summary>
    /// UI层的交易确认服务实现
    /// UI-layer transaction confirmation service implementation
    /// </summary>
    public class UITransactionConfirmationService : ITransactionConfirmationService
    {
        private readonly IUserSettingsService _userSettingsService;
        private readonly IMultiFactorAuthManager _mfaManager;
        private readonly IBiometricAuthService _biometricAuthService;
        private readonly ILogger<UITransactionConfirmationService> _logger;

        public UITransactionConfirmationService(
            IUserSettingsService userSettingsService,
            IMultiFactorAuthManager mfaManager,
            IBiometricAuthService biometricAuthService,
            ILogger<UITransactionConfirmationService> logger)
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

                // 创建确认页面 / Create confirmation page
                var viewModel = new TransactionConfirmationViewModel(
                    _mfaManager,
                    _biometricAuthService,
                    _userSettingsService,
                    _logger);

                var page = new TransactionConfirmationPage(viewModel, _logger);

                // 设置交易详情 / Set transaction details
                viewModel.SetTransactionDetails(fromAddress, toAddress, amount, currency, fee);

                // 显示确认页面 / Show confirmation page
                await Shell.Current.Navigation.PushModalAsync(page);

                // 等待用户确认 / Wait for user confirmation
                return await viewModel.GetConfirmationResult();
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