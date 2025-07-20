using Microsoft.Maui.Controls;
using MultiChainWallet.Infrastructure.Services;
using Plugin.Fingerprint.Abstractions;
using System;
using System.Threading.Tasks;

namespace MultiChainWallet.UI.Pages
{
    /// <summary>
    /// 生物识别设置页面
    /// Biometric Settings Page
    /// </summary>
    public partial class BiometricSettingsPage : ContentPage
    {
        private readonly IBiometricService _biometricService;
        private bool _isDeviceSupported = false;
        private BiometricType _supportedBiometricType = BiometricType.None;

        /// <summary>
        /// 指纹识别开关状态
        /// Fingerprint recognition toggle state
        /// </summary>
        public bool IsFingerprintEnabled { get; set; }

        /// <summary>
        /// 面容识别开关状态
        /// Face ID toggle state
        /// </summary>
        public bool IsFaceIdEnabled { get; set; }

        /// <summary>
        /// 转账验证开关状态
        /// Transfer verification toggle state
        /// </summary>
        public bool IsTransferVerificationEnabled { get; set; }

        /// <summary>
        /// 代币交换验证开关状态
        /// Token swap verification toggle state
        /// </summary>
        public bool IsSwapVerificationEnabled { get; set; }

        /// <summary>
        /// 钱包解锁验证开关状态
        /// Wallet unlock verification toggle state
        /// </summary>
        public bool IsUnlockVerificationEnabled { get; set; }

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="biometricService">生物识别服务</param>
        public BiometricSettingsPage(IBiometricService biometricService)
        {
            InitializeComponent();
            _biometricService = biometricService;
            BindingContext = this;

            // 禁用测试按钮，直到检查完生物识别支持
            // Disable test button until biometric support check completes
            TestButton.IsEnabled = false;
            
            // 检查生物识别支持并加载设置
            // Check biometric support and load settings
            CheckBiometricSupport();
            LoadSettings();
        }

        /// <summary>
        /// 检查设备是否支持生物识别
        /// Check if device supports biometrics
        /// </summary>
        private async void CheckBiometricSupport()
        {
            try
            {
                _isDeviceSupported = await _biometricService.IsBiometricSupportedAsync();
                
                if (_isDeviceSupported)
                {
                    // 获取支持的生物识别类型
                    // Get the supported biometric type
                    _supportedBiometricType = await _biometricService.GetBiometricTypeAsync();
                    
                    // 根据生物识别类型更新UI
                    // Update UI based on biometric type
                    UpdateUIForBiometricType();
                }
                else
                {
                    SupportStatusLabel.Text = "您的设备不支持生物识别 | Your device does not support biometrics";
                    SupportStatusLabel.TextColor = Colors.Red;
                    
                    // 禁用所有开关
                    // Disable all switches
                    FingerprintToggle.IsEnabled = false;
                    FaceIdToggle.IsEnabled = false;
                    TransferToggle.IsEnabled = false;
                    SwapToggle.IsEnabled = false;
                    UnlockToggle.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                SupportStatusLabel.Text = $"检查生物识别支持时出错 | Error checking biometric support: {ex.Message}";
                SupportStatusLabel.TextColor = Colors.Red;
            }
        }

        /// <summary>
        /// 根据生物识别类型更新UI
        /// Update UI based on biometric type
        /// </summary>
        private void UpdateUIForBiometricType()
        {
            string biometricTypeText = _supportedBiometricType switch
            {
                BiometricType.Fingerprint => "指纹识别 | Fingerprint",
                BiometricType.Face => "面部识别 | Face ID",
                BiometricType.Iris => "虹膜识别 | Iris",
                _ => "生物识别 | Biometrics"
            };
            
            SupportStatusLabel.Text = $"您的设备支持{biometricTypeText} | Your device supports {biometricTypeText}";
            SupportStatusLabel.TextColor = Colors.Green;
            TestButton.IsEnabled = true;
            
            // 根据支持的类型启用或禁用开关
            // Enable or disable toggles based on supported type
            FingerprintToggle.IsEnabled = _supportedBiometricType == BiometricType.Fingerprint;
            FaceIdToggle.IsEnabled = _supportedBiometricType == BiometricType.Face;
            
            // 如果设备仅支持一种生物识别类型，则禁用另一种类型的开关
            // If the device only supports one type of biometrics, disable the toggle for the other type
            if (_supportedBiometricType == BiometricType.Fingerprint)
            {
                FaceIdToggle.IsToggled = false;
                FaceIdToggle.IsEnabled = false;
            }
            else if (_supportedBiometricType == BiometricType.Face)
            {
                FingerprintToggle.IsToggled = false;
                FingerprintToggle.IsEnabled = false;
            }
            
            // 启用功能开关
            // Enable feature toggles
            TransferToggle.IsEnabled = true;
            SwapToggle.IsEnabled = true;
            UnlockToggle.IsEnabled = true;
        }

        /// <summary>
        /// 加载保存的设置
        /// Load saved settings
        /// </summary>
        private void LoadSettings()
        {
            // 从首选项中加载设置
            // Load settings from preferences
            IsFingerprintEnabled = Preferences.Default.Get("biometrics_fingerprint_enabled", false);
            IsFaceIdEnabled = Preferences.Default.Get("biometrics_faceid_enabled", false);
            IsTransferVerificationEnabled = Preferences.Default.Get("biometrics_transfer_verification", false);
            IsSwapVerificationEnabled = Preferences.Default.Get("biometrics_swap_verification", false);
            IsUnlockVerificationEnabled = Preferences.Default.Get("biometrics_unlock_verification", false);
            
            // 更新UI
            // Update UI
            FingerprintToggle.IsToggled = IsFingerprintEnabled;
            FaceIdToggle.IsToggled = IsFaceIdEnabled;
            TransferToggle.IsToggled = IsTransferVerificationEnabled;
            SwapToggle.IsToggled = IsSwapVerificationEnabled;
            UnlockToggle.IsToggled = IsUnlockVerificationEnabled;
        }

        /// <summary>
        /// 保存设置
        /// Save settings
        /// </summary>
        private void SaveSettings()
        {
            // 保存设置到首选项
            // Save settings to preferences
            Preferences.Default.Set("biometrics_fingerprint_enabled", IsFingerprintEnabled);
            Preferences.Default.Set("biometrics_faceid_enabled", IsFaceIdEnabled);
            Preferences.Default.Set("biometrics_transfer_verification", IsTransferVerificationEnabled);
            Preferences.Default.Set("biometrics_swap_verification", IsSwapVerificationEnabled);
            Preferences.Default.Set("biometrics_unlock_verification", IsUnlockVerificationEnabled);
            
            // 更新主设置中的总开关状态
            // Update main biometrics toggle state in settings
            Preferences.Default.Set("biometrics_enabled", IsFingerprintEnabled || IsFaceIdEnabled);
        }

        /// <summary>
        /// 指纹识别开关状态变化事件
        /// Fingerprint toggle changed event
        /// </summary>
        private void OnFingerprintToggled(object sender, ToggledEventArgs e)
        {
            IsFingerprintEnabled = e.Value;
        }

        /// <summary>
        /// 面容识别开关状态变化事件
        /// Face ID toggle changed event
        /// </summary>
        private void OnFaceIdToggled(object sender, ToggledEventArgs e)
        {
            IsFaceIdEnabled = e.Value;
        }

        /// <summary>
        /// 测试生物识别按钮点击事件
        /// Test biometrics button click event
        /// </summary>
        private async void OnTestButtonClicked(object sender, EventArgs e)
        {
            if (!_isDeviceSupported)
            {
                await DisplayAlert("错误 / Error", 
                    "您的设备不支持生物识别 / Your device does not support biometrics", 
                    "确定 / OK");
                return;
            }

            try
            {
                string biometricTypeText = _supportedBiometricType switch
                {
                    BiometricType.Fingerprint => "指纹识别 | Fingerprint",
                    BiometricType.Face => "面部识别 | Face ID",
                    BiometricType.Iris => "虹膜识别 | Iris",
                    _ => "生物识别 | Biometrics"
                };
                
                await DisplayAlert("开始测试 / Begin Test", 
                    $"请准备使用{biometricTypeText}进行验证 / Please prepare to authenticate using {biometricTypeText}", 
                    "确定 / OK");
                
                // 使用钱包解锁场景进行测试
                // Test using wallet unlock scenario
                var result = await _biometricService.AuthenticateForWalletUnlockAsync();
                
                if (result.Authenticated)
                {
                    await DisplayAlert("成功 / Success", 
                        "生物识别认证成功 / Biometric authentication successful", 
                        "确定 / OK");
                }
                else
                {
                    await DisplayAlert("失败 / Failed", 
                        $"生物识别认证失败: {result.Status} / Biometric authentication failed: {result.Status}", 
                        "确定 / OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误 / Error", 
                    $"生物识别认证过程中出错: {ex.Message} / Error during biometric authentication: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 保存设置按钮点击事件
        /// Save settings button click event
        /// </summary>
        private async void OnSaveButtonClicked(object sender, EventArgs e)
        {
            SaveSettings();
            await DisplayAlert("成功 / Success", 
                "生物识别设置已保存 / Biometric settings have been saved", 
                "确定 / OK");
            
            await Shell.Current.GoToAsync("..");
        }
    }
} 