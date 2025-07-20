using System.Windows.Input;
using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Interfaces;
using MultiChainWallet.Core.Models;
using ZXing.QrCode;

namespace MultiChainWallet.UI.ViewModels
{
    public class SecuritySettingsViewModel : BaseViewModel
    {
        private readonly IMultiFactorAuthManager _mfaManager;
        private readonly IBiometricAuthService _biometricAuthService;
        private readonly ITotpService _totpService;
        private readonly IUserSettingsService _userSettingsService;
        private readonly ISecurityService _securityService;
        private readonly ILogger<SecuritySettingsViewModel> _logger;

        private bool _isPasswordVerificationRequired = true;
        private string _password;
        private bool _isBiometricEnabled;
        private bool _isBiometricAvailable;
        private string _biometricStatusText;
        private bool _isTotpEnabled;
        private string _totpStatusText;
        private bool _showTotpSetup;
        private string _totpSecretKey;
        private string _totpVerificationCode;
        private ImageSource _totpQrCodeImageSource;
        private bool _isAutoLockEnabled;
        private int _autoLockTimeout = 5;
        private bool _isTransactionConfirmationEnabled = true;

        public SecuritySettingsViewModel(
            IMultiFactorAuthManager mfaManager,
            IBiometricAuthService biometricAuthService,
            ITotpService totpService,
            IUserSettingsService userSettingsService,
            ISecurityService securityService,
            ILogger<SecuritySettingsViewModel> logger)
        {
            _mfaManager = mfaManager;
            _biometricAuthService = biometricAuthService;
            _totpService = totpService;
            _userSettingsService = userSettingsService;
            _securityService = securityService;
            _logger = logger;

            // 初始化命令 / Initialize commands
            VerifyPasswordCommand = new Command(async () => await VerifyPasswordAsync());
            TestBiometricCommand = new Command(async () => await TestBiometricAsync());
            VerifyTotpCommand = new Command(async () => await VerifyTotpAsync());
            TestTotpCommand = new Command(async () => await TestTotpAsync());
            DisableTotpCommand = new Command(async () => await DisableTotpAsync());
            SaveSettingsCommand = new Command(async () => await SaveSettingsAsync());
        }

        #region Properties

        public bool IsPasswordVerificationRequired
        {
            get => _isPasswordVerificationRequired;
            set => SetProperty(ref _isPasswordVerificationRequired, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public bool IsBiometricEnabled
        {
            get => _isBiometricEnabled;
            set
            {
                if (SetProperty(ref _isBiometricEnabled, value))
                {
                    _ = HandleBiometricToggleAsync(value);
                }
            }
        }

        public bool IsBiometricAvailable
        {
            get => _isBiometricAvailable;
            set => SetProperty(ref _isBiometricAvailable, value);
        }

        public string BiometricStatusText
        {
            get => _biometricStatusText;
            set => SetProperty(ref _biometricStatusText, value);
        }

        public bool IsTotpEnabled
        {
            get => _isTotpEnabled;
            set
            {
                if (SetProperty(ref _isTotpEnabled, value))
                {
                    _ = HandleTotpToggleAsync(value);
                }
            }
        }

        public string TotpStatusText
        {
            get => _totpStatusText;
            set => SetProperty(ref _totpStatusText, value);
        }

        public bool ShowTotpSetup
        {
            get => _showTotpSetup;
            set => SetProperty(ref _showTotpSetup, value);
        }

        public string TotpSecretKey
        {
            get => _totpSecretKey;
            set => SetProperty(ref _totpSecretKey, value);
        }

        public string TotpVerificationCode
        {
            get => _totpVerificationCode;
            set => SetProperty(ref _totpVerificationCode, value);
        }

        public ImageSource TotpQrCodeImageSource
        {
            get => _totpQrCodeImageSource;
            set => SetProperty(ref _totpQrCodeImageSource, value);
        }

        public bool IsAutoLockEnabled
        {
            get => _isAutoLockEnabled;
            set => SetProperty(ref _isAutoLockEnabled, value);
        }

        public int AutoLockTimeout
        {
            get => _autoLockTimeout;
            set => SetProperty(ref _autoLockTimeout, value);
        }

        public bool IsTransactionConfirmationEnabled
        {
            get => _isTransactionConfirmationEnabled;
            set => SetProperty(ref _isTransactionConfirmationEnabled, value);
        }

        #endregion

        #region Commands

        public ICommand VerifyPasswordCommand { get; }
        public ICommand TestBiometricCommand { get; }
        public ICommand VerifyTotpCommand { get; }
        public ICommand TestTotpCommand { get; }
        public ICommand DisableTotpCommand { get; }
        public ICommand SaveSettingsCommand { get; }

        #endregion

        #region Public Methods

        public async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;

                // 检查生物识别可用性 / Check biometric availability
                IsBiometricAvailable = await _biometricAuthService.IsBiometricAvailableAsync();
                BiometricStatusText = IsBiometricAvailable ? 
                    "可用 / Available" : 
                    "不可用 / Not Available";

                // 加载当前设置 / Load current settings
                var settings = await _userSettingsService.GetSecuritySettingsAsync();
                var mfaStatus = await _mfaManager.GetMfaStatusAsync();

                IsBiometricEnabled = mfaStatus.IsBiometricEnabled;
                IsTotpEnabled = mfaStatus.IsTotpEnabled;
                IsAutoLockEnabled = settings.IsAutoLockEnabled;
                AutoLockTimeout = settings.AutoLockTimeout;
                IsTransactionConfirmationEnabled = settings.IsTransactionConfirmationEnabled;

                UpdateTotpStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化安全设置时出错 / Error initializing security settings");
                await Application.Current.MainPage.DisplayAlert(
                    "错误 / Error",
                    "加载设置时出错 / Error loading settings",
                    "确定 / OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void Cleanup()
        {
            // 清理敏感数据 / Clean up sensitive data
            Password = null;
            TotpSecretKey = null;
            TotpVerificationCode = null;
        }

        #endregion

        #region Private Methods

        private async Task VerifyPasswordAsync()
        {
            if (string.IsNullOrEmpty(Password))
            {
                await ShowAlert("错误 / Error", "请输入密码 / Please enter password");
                return;
            }

            try
            {
                IsBusy = true;
                var storedHash = await _userSettingsService.GetPasswordHashAsync();
                var isValid = await _securityService.VerifyPasswordHashAsync(Password, storedHash);

                if (isValid)
                {
                    IsPasswordVerificationRequired = false;
                }
                else
                {
                    await ShowAlert("错误 / Error", "密码错误 / Invalid password");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证密码时出错 / Error verifying password");
                await ShowAlert("错误 / Error", "验证密码时出错 / Error verifying password");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task HandleBiometricToggleAsync(bool enabled)
        {
            if (enabled)
            {
                var result = await _mfaManager.SetupMfaAsync(MfaType.Biometric, Password);
                if (!result.Success)
                {
                    IsBiometricEnabled = false;
                    await ShowAlert("错误 / Error", result.ErrorMessage);
                }
            }
            else
            {
                var success = await _mfaManager.DisableMfaAsync(MfaType.Biometric, Password);
                if (!success)
                {
                    IsBiometricEnabled = true;
                    await ShowAlert("错误 / Error", "禁用生物识别失败 / Failed to disable biometric");
                }
            }
        }

        private async Task TestBiometricAsync()
        {
            try
            {
                IsBusy = true;
                var success = await _mfaManager.VerifyMfaAsync(MfaType.Biometric);
                await ShowAlert(
                    "测试结果 / Test Result",
                    success ? "生物识别验证成功 / Biometric verification successful" :
                             "生物识别验证失败 / Biometric verification failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试生物识别时出错 / Error testing biometric");
                await ShowAlert("错误 / Error", "测试生物识别时出错 / Error testing biometric");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task HandleTotpToggleAsync(bool enabled)
        {
            if (enabled)
            {
                ShowTotpSetup = true;
                await SetupTotpAsync();
            }
            else
            {
                ShowTotpSetup = false;
                TotpSecretKey = null;
                TotpQrCodeImageSource = null;
            }
        }

        private async Task SetupTotpAsync()
        {
            try
            {
                IsBusy = true;

                // 生成新的TOTP密钥 / Generate new TOTP secret
                TotpSecretKey = await _totpService.GenerateSecretKeyAsync();

                // 生成QR码URI / Generate QR code URI
                var qrUri = _totpService.GenerateQrCodeUri(
                    TotpSecretKey,
                    "MultiChainWallet",
                    "MultiChainWallet");

                // 生成QR码图像 / Generate QR code image
                var qrWriter = new QRCodeWriter();
                var matrix = qrWriter.encode(qrUri, ZXing.BarcodeFormat.QR_CODE, 300, 300);
                var bitmap = new ZXing.Common.BitMatrix(matrix.Width, matrix.Height);
                
                for (int x = 0; x < matrix.Width; x++)
                {
                    for (int y = 0; y < matrix.Height; y++)
                    {
                        bitmap[x, y] = matrix[x, y];
                    }
                }

                // 将QR码转换为图像 / Convert QR code to image
                var renderer = new ZXing.Rendering.PixelDataRenderer();
                var pixelData = renderer.Render(bitmap, ZXing.BarcodeFormat.QR_CODE, null);
                
                // 创建MAUI图像 / Create MAUI image
                var stream = new MemoryStream(pixelData.Pixels);
                TotpQrCodeImageSource = ImageSource.FromStream(() => stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置TOTP时出错 / Error setting up TOTP");
                await ShowAlert("错误 / Error", "设置TOTP时出错 / Error setting up TOTP");
                IsTotpEnabled = false;
                ShowTotpSetup = false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task VerifyTotpAsync()
        {
            if (string.IsNullOrEmpty(TotpVerificationCode))
            {
                await ShowAlert("错误 / Error", "请输入验证码 / Please enter verification code");
                return;
            }

            try
            {
                IsBusy = true;
                var result = await _mfaManager.SetupMfaAsync(MfaType.Totp, Password);

                if (result.Success)
                {
                    ShowTotpSetup = false;
                    UpdateTotpStatus();
                    await ShowAlert("成功 / Success", "TOTP设置成功 / TOTP setup successful");
                }
                else
                {
                    IsTotpEnabled = false;
                    await ShowAlert("错误 / Error", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证TOTP时出错 / Error verifying TOTP");
                await ShowAlert("错误 / Error", "验证TOTP时出错 / Error verifying TOTP");
                IsTotpEnabled = false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task TestTotpAsync()
        {
            try
            {
                IsBusy = true;
                var promptResult = await Application.Current.MainPage.DisplayPromptAsync(
                    "测试TOTP / Test TOTP",
                    "请输入验证码 / Please enter verification code",
                    "验证 / Verify",
                    "取消 / Cancel");

                if (string.IsNullOrEmpty(promptResult))
                    return;

                var success = await _mfaManager.VerifyMfaAsync(MfaType.Totp, promptResult);
                await ShowAlert(
                    "测试结果 / Test Result",
                    success ? "TOTP验证成功 / TOTP verification successful" :
                             "TOTP验证失败 / TOTP verification failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试TOTP时出错 / Error testing TOTP");
                await ShowAlert("错误 / Error", "测试TOTP时出错 / Error testing TOTP");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DisableTotpAsync()
        {
            try
            {
                IsBusy = true;
                var success = await _mfaManager.DisableMfaAsync(MfaType.Totp, Password);

                if (success)
                {
                    IsTotpEnabled = false;
                    ShowTotpSetup = false;
                    UpdateTotpStatus();
                    await ShowAlert("成功 / Success", "TOTP已禁用 / TOTP disabled");
                }
                else
                {
                    await ShowAlert("错误 / Error", "禁用TOTP失败 / Failed to disable TOTP");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "禁用TOTP时出错 / Error disabling TOTP");
                await ShowAlert("错误 / Error", "禁用TOTP时出错 / Error disabling TOTP");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                IsBusy = true;

                var settings = new SecuritySettings
                {
                    IsAutoLockEnabled = IsAutoLockEnabled,
                    AutoLockTimeout = AutoLockTimeout,
                    IsTransactionConfirmationEnabled = IsTransactionConfirmationEnabled,
                    LastUpdated = DateTime.UtcNow
                };

                await _userSettingsService.SaveSecuritySettingsAsync(settings);
                await ShowAlert("成功 / Success", "设置已保存 / Settings saved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存设置时出错 / Error saving settings");
                await ShowAlert("错误 / Error", "保存设置时出错 / Error saving settings");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateTotpStatus()
        {
            TotpStatusText = IsTotpEnabled ?
                "已启用 / Enabled" :
                "未启用 / Not Enabled";
        }

        private async Task ShowAlert(string title, string message)
        {
            await Application.Current.MainPage.DisplayAlert(title, message, "确定 / OK");
        }

        #endregion
    }
} 