using System.Windows.Input;
using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Interfaces;
using MultiChainWallet.Core.Models;

namespace MultiChainWallet.UI.ViewModels
{
    public class TransactionConfirmationViewModel : BaseViewModel
    {
        private readonly IMultiFactorAuthManager _mfaManager;
        private readonly IBiometricAuthService _biometricAuthService;
        private readonly IUserSettingsService _userSettingsService;
        private readonly ILogger<TransactionConfirmationViewModel> _logger;

        private string _fromAddress;
        private string _toAddress;
        private string _amount;
        private string _currency;
        private string _fee;
        private bool _useBiometric;
        private bool _useTotp;
        private bool _isBiometricAvailable;
        private bool _isTotpEnabled;
        private string _totpCode;
        private TaskCompletionSource<bool> _confirmationResult;

        public TransactionConfirmationViewModel(
            IMultiFactorAuthManager mfaManager,
            IBiometricAuthService biometricAuthService,
            IUserSettingsService userSettingsService,
            ILogger<TransactionConfirmationViewModel> logger)
        {
            _mfaManager = mfaManager;
            _biometricAuthService = biometricAuthService;
            _userSettingsService = userSettingsService;
            _logger = logger;

            // 初始化命令 / Initialize commands
            ConfirmTransactionCommand = new Command(async () => await ConfirmTransactionAsync());
            CancelCommand = new Command(async () => await CancelAsync());
        }

        #region Properties

        public string FromAddress
        {
            get => _fromAddress;
            set => SetProperty(ref _fromAddress, value);
        }

        public string ToAddress
        {
            get => _toAddress;
            set => SetProperty(ref _toAddress, value);
        }

        public string Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        public string Currency
        {
            get => _currency;
            set => SetProperty(ref _currency, value);
        }

        public string Fee
        {
            get => _fee;
            set => SetProperty(ref _fee, value);
        }

        public bool UseBiometric
        {
            get => _useBiometric;
            set
            {
                if (SetProperty(ref _useBiometric, value) && value)
                {
                    UseTotp = false;
                }
            }
        }

        public bool UseTotp
        {
            get => _useTotp;
            set
            {
                if (SetProperty(ref _useTotp, value) && value)
                {
                    UseBiometric = false;
                }
            }
        }

        public bool IsBiometricAvailable
        {
            get => _isBiometricAvailable;
            set => SetProperty(ref _isBiometricAvailable, value);
        }

        public bool IsTotpEnabled
        {
            get => _isTotpEnabled;
            set => SetProperty(ref _isTotpEnabled, value);
        }

        public string TotpCode
        {
            get => _totpCode;
            set => SetProperty(ref _totpCode, value);
        }

        #endregion

        #region Commands

        public ICommand ConfirmTransactionCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region Public Methods

        public async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;

                // 检查可用的认证方式 / Check available authentication methods
                IsBiometricAvailable = await _biometricAuthService.IsBiometricAvailableAsync();
                var mfaStatus = await _mfaManager.GetMfaStatusAsync();
                IsTotpEnabled = mfaStatus.IsTotpEnabled;

                // 设置默认认证方式 / Set default authentication method
                if (IsBiometricAvailable)
                {
                    UseBiometric = true;
                }
                else if (IsTotpEnabled)
                {
                    UseTotp = true;
                }

                _confirmationResult = new TaskCompletionSource<bool>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化交易确认时出错 / Error initializing transaction confirmation");
                await ShowAlert("错误 / Error", "初始化交易确认时出错 / Error initializing transaction confirmation");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void SetTransactionDetails(string from, string to, string amount, string currency, string fee)
        {
            FromAddress = from;
            ToAddress = to;
            Amount = amount;
            Currency = currency;
            Fee = fee;
        }

        public Task<bool> GetConfirmationResult()
        {
            return _confirmationResult?.Task ?? Task.FromResult(false);
        }

        public void Cleanup()
        {
            TotpCode = null;
        }

        #endregion

        #region Private Methods

        private async Task ConfirmTransactionAsync()
        {
            if (!UseBiometric && !UseTotp)
            {
                await ShowAlert("错误 / Error", "请选择认证方式 / Please select an authentication method");
                return;
            }

            try
            {
                IsBusy = true;
                bool isVerified = false;

                if (UseBiometric)
                {
                    isVerified = await _mfaManager.VerifyMfaAsync(MfaType.Biometric);
                }
                else if (UseTotp)
                {
                    if (string.IsNullOrEmpty(TotpCode))
                    {
                        await ShowAlert("错误 / Error", "请输入验证码 / Please enter verification code");
                        return;
                    }
                    isVerified = await _mfaManager.VerifyMfaAsync(MfaType.Totp, TotpCode);
                }

                if (isVerified)
                {
                    _confirmationResult?.TrySetResult(true);
                    await Shell.Current.Navigation.PopModalAsync();
                }
                else
                {
                    await ShowAlert("错误 / Error", "验证失败 / Verification failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "确认交易时出错 / Error confirming transaction");
                await ShowAlert("错误 / Error", "确认交易时出错 / Error confirming transaction");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CancelAsync()
        {
            _confirmationResult?.TrySetResult(false);
            await Shell.Current.Navigation.PopModalAsync();
        }

        private async Task ShowAlert(string title, string message)
        {
            await Application.Current.MainPage.DisplayAlert(title, message, "确定 / OK");
        }

        #endregion
    }
} 