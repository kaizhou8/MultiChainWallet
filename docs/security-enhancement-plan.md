# MultiChainWallet 安全功能增强计划 / Security Enhancement Plan

*创建日期 / Creation Date: 2025-04-01*
*最后更新 / Last Updated: 2025-04-01*

## 概述 / Overview

本文档详细说明了 MultiChainWallet 应用的安全功能增强计划，重点是实现生物识别与多因素认证集成，以提供更高级别的安全保护，同时保持良好的用户体验。

This document details the security enhancement plan for the MultiChainWallet application, focusing on implementing biometric integration and multi-factor authentication to provide a higher level of security protection while maintaining a good user experience.

## 安全目标 / Security Objectives

1. **增强用户身份验证 / Enhanced User Authentication**
   - 集成生物识别技术（指纹/面部识别）
   - 实现基于时间的一次性密码 (TOTP) 双因素认证
   - 开发多因素认证流程管理

2. **提高关键操作安全性 / Improved Security for Critical Operations**
   - 为私钥访问添加额外的身份验证层
   - 为交易签名添加多因素验证
   - 为钱包恢复和备份操作增加安全验证

3. **保持用户友好性 / Maintain User Friendliness**
   - 提供直观的身份验证流程
   - 实现合理的回退机制
   - 确保认证过程高效且无缝

## 实施方案 / Implementation Approach

### 一、Windows Hello 生物识别集成 / Windows Hello Biometric Integration

利用 Windows.Security.Credentials API 集成 Windows Hello 生物识别功能：

```csharp
// BiometricAuthService.cs
public class BiometricAuthService : IBiometricAuthService
{
    private readonly ILogger<BiometricAuthService> _logger;
    
    public BiometricAuthService(ILogger<BiometricAuthService> logger)
    {
        _logger = logger;
    }
    
    public async Task<bool> IsBiometricAvailableAsync()
    {
        try
        {
            // 检查设备是否支持 Windows Hello
            // Check if the device supports Windows Hello
            var availabilityResult = await UserConsentVerifier.CheckAvailabilityAsync();
            return availabilityResult == UserConsentVerifierAvailability.Available;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查生物识别可用性时出错 / Error checking biometric availability");
            return false;
        }
    }
    
    public async Task<bool> AuthenticateAsync(string message)
    {
        try
        {
            // 请求用户进行生物识别验证
            // Request user to perform biometric verification
            var consentResult = await UserConsentVerifier.RequestVerificationAsync(message);
            return consentResult == UserConsentVerifierResult.Verified;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生物识别认证过程中出错 / Error during biometric authentication");
            return false;
        }
    }
    
    public async Task<(bool Success, string DeviceId)> RegisterDeviceAsync()
    {
        try
        {
            // 生成设备ID并与生物识别关联
            // Generate device ID and associate with biometrics
            string deviceId = Guid.NewGuid().ToString();
            
            // 请求用户进行生物识别验证以关联设备
            // Request user to perform biometric verification to associate device
            var consentResult = await UserConsentVerifier.RequestVerificationAsync(
                "请使用生物识别验证以注册此设备 / Please use biometric verification to register this device");
                
            if (consentResult == UserConsentVerifierResult.Verified)
            {
                // 存储设备ID（加密）
                // Store device ID (encrypted)
                return (true, deviceId);
            }
            
            return (false, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册设备时出错 / Error registering device");
            return (false, string.Empty);
        }
    }
}
```

### 二、TOTP 双因素认证实现 / TOTP Two-Factor Authentication Implementation

使用 OtpNet 库实现基于时间的一次性密码 (TOTP) 功能：

```csharp
// TotpService.cs
public class TotpService : ITotpService
{
    private readonly ISecurityService _securityService;
    private readonly ILogger<TotpService> _logger;
    
    public TotpService(ISecurityService securityService, ILogger<TotpService> logger)
    {
        _securityService = securityService;
        _logger = logger;
    }
    
    public async Task<string> GenerateSecretKeyAsync()
    {
        try
        {
            // 生成安全随机密钥
            // Generate secure random key
            byte[] secretKey = _securityService.GenerateSecureRandomBytes(20);
            
            // 转换为Base32格式
            // Convert to Base32 format
            string base32Secret = Base32Encoding.ToString(secretKey);
            
            // 加密存储密钥
            // Encrypt and store the key
            return base32Secret;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成TOTP密钥时出错 / Error generating TOTP key");
            throw;
        }
    }
    
    public string GenerateQrCodeUri(string secretKey, string accountName, string issuer)
    {
        // 生成用于扫描的二维码URI
        // Generate QR code URI for scanning
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(accountName)}?secret={secretKey}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";
    }
    
    public bool VerifyCode(string secretKey, string code)
    {
        try
        {
            // 解码密钥
            // Decode the key
            byte[] key = Base32Encoding.ToBytes(secretKey);
            
            // 创建TOTP对象
            // Create TOTP object
            var totp = new Totp(key);
            
            // 验证代码，允许前后1个时间窗口的代码（共90秒窗口）
            // Verify code, allowing codes from 1 window before and after (90 second window)
            return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证TOTP代码时出错 / Error verifying TOTP code");
            return false;
        }
    }
}
```

### 三、多因素认证流程管理器 / Multi-Factor Authentication Flow Manager

创建统一的多因素认证流程管理器，协调不同的认证方法：

```csharp
// MultiFactorAuthManager.cs
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
    
    public async Task<bool> IsSetupCompleteAsync()
    {
        // 检查用户是否已完成MFA设置
        // Check if user has completed MFA setup
        var settings = await _userSettingsService.GetSecuritySettingsAsync();
        return settings.IsBiometricEnabled || settings.IsTotpEnabled;
    }
    
    public async Task<MfaSetupResult> SetupMfaAsync(MfaType type, string password)
    {
        try
        {
            // 验证密码
            // Verify password
            if (!await _securityService.VerifyPasswordHashAsync(password, await _userSettingsService.GetPasswordHashAsync()))
            {
                return new MfaSetupResult { Success = false, ErrorMessage = "密码不正确 / Password is incorrect" };
            }
            
            var settings = await _userSettingsService.GetSecuritySettingsAsync();
            
            switch (type)
            {
                case MfaType.Biometric:
                    if (!await _biometricAuthService.IsBiometricAvailableAsync())
                    {
                        return new MfaSetupResult { Success = false, ErrorMessage = "此设备不支持生物识别 / Biometrics not supported on this device" };
                    }
                    
                    var (success, deviceId) = await _biometricAuthService.RegisterDeviceAsync();
                    if (success)
                    {
                        settings.IsBiometricEnabled = true;
                        settings.BiometricDeviceId = deviceId;
                        await _userSettingsService.SaveSecuritySettingsAsync(settings);
                        return new MfaSetupResult { Success = true };
                    }
                    return new MfaSetupResult { Success = false, ErrorMessage = "生物识别注册失败 / Biometric registration failed" };
                    
                case MfaType.Totp:
                    string secretKey = await _totpService.GenerateSecretKeyAsync();
                    string qrCodeUri = _totpService.GenerateQrCodeUri(secretKey, settings.UserEmail, "MultiChainWallet");
                    
                    settings.TotpSecretKey = await _securityService.EncryptAsync(
                        System.Text.Encoding.UTF8.GetBytes(secretKey), password);
                    settings.IsTotpEnabled = true;
                    await _userSettingsService.SaveSecuritySettingsAsync(settings);
                    
                    return new MfaSetupResult 
                    { 
                        Success = true, 
                        TotpSecretKey = secretKey,
                        TotpQrCodeUri = qrCodeUri
                    };
                    
                default:
                    return new MfaSetupResult { Success = false, ErrorMessage = "不支持的MFA类型 / Unsupported MFA type" };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置MFA时出错 / Error setting up MFA");
            return new MfaSetupResult { Success = false, ErrorMessage = "设置MFA时发生错误 / Error occurred while setting up MFA" };
        }
    }
    
    public async Task<bool> AuthenticateAsync(MfaType type, string verificationData)
    {
        try
        {
            var settings = await _userSettingsService.GetSecuritySettingsAsync();
            
            switch (type)
            {
                case MfaType.Biometric:
                    if (!settings.IsBiometricEnabled)
                    {
                        return false;
                    }
                    
                    return await _biometricAuthService.AuthenticateAsync(
                        "请使用生物识别验证您的身份 / Please verify your identity using biometrics");
                    
                case MfaType.Totp:
                    if (!settings.IsTotpEnabled)
                    {
                        return false;
                    }
                    
                    // 解密TOTP密钥
                    // Decrypt TOTP key
                    string secretKey = System.Text.Encoding.UTF8.GetString(
                        await _securityService.DecryptAsync(settings.TotpSecretKey, verificationData));
                    
                    // 验证用户提供的TOTP代码
                    // Verify TOTP code provided by user
                    return _totpService.VerifyCode(secretKey, verificationData);
                    
                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MFA认证过程中出错 / Error during MFA authentication");
            return false;
        }
    }
    
    public async Task<bool> RequiresMfaForActionAsync(SecurityAction action)
    {
        // 根据操作类型和安全设置确定是否需要MFA
        // Determine if MFA is required based on action type and security settings
        var settings = await _userSettingsService.GetSecuritySettingsAsync();
        
        switch (action)
        {
            case SecurityAction.AccessWallet:
                return settings.RequireMfaForWalletAccess;
                
            case SecurityAction.SignTransaction:
                return settings.RequireMfaForTransactions;
                
            case SecurityAction.ExportPrivateKey:
                return true; // 始终为私钥导出要求MFA / Always require MFA for private key export
                
            case SecurityAction.ChangeSettings:
                return settings.RequireMfaForSettingsChange;
                
            default:
                return false;
        }
    }
}
```

### 四、安全设置界面 / Security Settings Interface

创建用户友好的安全设置界面，允许用户配置多因素认证选项：

```csharp
// SecuritySettingsPage.xaml.cs
public partial class SecuritySettingsPage : ContentPage
{
    private readonly IMultiFactorAuthManager _mfaManager;
    private readonly IUserSettingsService _userSettingsService;
    private readonly ILogger<SecuritySettingsPage> _logger;
    
    public SecuritySettingsPage(
        IMultiFactorAuthManager mfaManager,
        IUserSettingsService userSettingsService,
        ILogger<SecuritySettingsPage> logger)
    {
        InitializeComponent();
        
        _mfaManager = mfaManager;
        _userSettingsService = userSettingsService;
        _logger = logger;
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSecuritySettings();
    }
    
    private async Task LoadSecuritySettings()
    {
        try
        {
            var settings = await _userSettingsService.GetSecuritySettingsAsync();
            
            // 更新UI以反映当前设置
            // Update UI to reflect current settings
            BiometricSwitch.IsToggled = settings.IsBiometricEnabled;
            TotpSwitch.IsToggled = settings.IsTotpEnabled;
            
            RequireMfaForWalletAccessSwitch.IsToggled = settings.RequireMfaForWalletAccess;
            RequireMfaForTransactionsSwitch.IsToggled = settings.RequireMfaForTransactions;
            RequireMfaForSettingsChangeSwitch.IsToggled = settings.RequireMfaForSettingsChange;
            
            // 检查生物识别可用性
            // Check biometric availability
            bool isBiometricAvailable = await _mfaManager.IsBiometricAvailableAsync();
            BiometricSwitch.IsEnabled = isBiometricAvailable;
            BiometricNotAvailableLabel.IsVisible = !isBiometricAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载安全设置时出错 / Error loading security settings");
            await DisplayAlert("错误 / Error", "无法加载安全设置 / Unable to load security settings", "确定 / OK");
        }
    }
    
    private async void BiometricSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        if (e.Value)
        {
            // 启用生物识别
            // Enable biometrics
            string password = await PromptForPassword();
            if (string.IsNullOrEmpty(password))
            {
                BiometricSwitch.IsToggled = false;
                return;
            }
            
            var result = await _mfaManager.SetupMfaAsync(MfaType.Biometric, password);
            if (!result.Success)
            {
                await DisplayAlert("错误 / Error", result.ErrorMessage, "确定 / OK");
                BiometricSwitch.IsToggled = false;
            }
        }
        else
        {
            // 禁用生物识别
            // Disable biometrics
            var settings = await _userSettingsService.GetSecuritySettingsAsync();
            settings.IsBiometricEnabled = false;
            await _userSettingsService.SaveSecuritySettingsAsync(settings);
        }
    }
    
    private async void TotpSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        if (e.Value)
        {
            // 启用TOTP
            // Enable TOTP
            string password = await PromptForPassword();
            if (string.IsNullOrEmpty(password))
            {
                TotpSwitch.IsToggled = false;
                return;
            }
            
            var result = await _mfaManager.SetupMfaAsync(MfaType.Totp, password);
            if (result.Success)
            {
                // 显示QR码和密钥
                // Show QR code and key
                await Navigation.PushAsync(new TotpSetupPage(result.TotpSecretKey, result.TotpQrCodeUri));
            }
            else
            {
                await DisplayAlert("错误 / Error", result.ErrorMessage, "确定 / OK");
                TotpSwitch.IsToggled = false;
            }
        }
        else
        {
            // 禁用TOTP
            // Disable TOTP
            var settings = await _userSettingsService.GetSecuritySettingsAsync();
            settings.IsTotpEnabled = false;
            await _userSettingsService.SaveSecuritySettingsAsync(settings);
        }
    }
    
    private async Task<string> PromptForPassword()
    {
        return await DisplayPromptAsync(
            "密码验证 / Password Verification",
            "请输入您的钱包密码以继续 / Please enter your wallet password to continue",
            "确定 / OK",
            "取消 / Cancel",
            placeholder: "密码 / Password",
            maxLength: 50,
            keyboard: Keyboard.Default,
            isPassword: true);
    }
    
    // 其他设置切换处理程序
    // Other settings toggle handlers
    // ...
}
```

## 安全层次结构 / Security Layer Structure

```
┌─────────────────────────────────────────┐
│         应用功能层 / Application Layer   │
├─────────────────────────────────────────┤
│       多因素认证层 / MFA Layer           │
│  ┌───────────────┐  ┌───────────────┐   │
│  │ 生物识别      │  │ TOTP双因素    │   │
│  │ Biometric     │  │ TOTP Two-     │   │
│  │ Authentication│  │ Factor Auth   │   │
│  └───────────────┘  └───────────────┘   │
│  ┌───────────────┐  ┌───────────────┐   │
│  │ 认证流程管理  │  │ 安全策略      │   │
│  │ Auth Flow     │  │ Security      │   │
│  │ Management    │  │ Policies      │   │
│  └───────────────┘  └───────────────┘   │
├─────────────────────────────────────────┤
│       现有安全层 / Existing Security     │
│  ┌───────────────┐  ┌───────────────┐   │
│  │ 自定义加密    │  │ 内存保护      │   │
│  │ Custom        │  │ Memory        │   │
│  │ Encryption    │  │ Protection    │   │
│  └───────────────┘  └───────────────┘   │
│  ┌───────────────┐  ┌───────────────┐   │
│  │ 完整性检查    │  │ 代码混淆      │   │
│  │ Integrity     │  │ Code          │   │
│  │ Checks        │  │ Obfuscation   │   │
│  └───────────────┘  └───────────────┘   │
└─────────────────────────────────────────┘
```

## 实施步骤 / Implementation Steps

1. **准备工作 / Preparation**
   - 添加必要的NuGet包（OtpNet等）
   - 创建接口和数据模型
   - 更新用户设置模型以支持MFA配置

2. **生物识别实现 / Biometric Implementation**
   - 实现BiometricAuthService
   - 添加Windows Hello集成
   - 创建生物识别注册流程

3. **TOTP实现 / TOTP Implementation**
   - 实现TotpService
   - 创建TOTP密钥生成和验证功能
   - 实现QR码生成功能

4. **多因素认证管理器实现 / MFA Manager Implementation**
   - 实现MultiFactorAuthManager
   - 创建认证流程协调逻辑
   - 实现安全策略管理

5. **UI集成 / UI Integration**
   - 创建安全设置页面
   - 实现TOTP设置向导
   - 更新关键操作流程以集成MFA验证

6. **测试和验证 / Testing and Verification**
   - 测试生物识别功能
   - 验证TOTP实现
   - 测试多因素认证流程
   - 验证回退机制

## 时间表 / Timeline

1. **阶段一：基础工作 (2025-04-01 - 2025-04-03)**
   - 设计接口和数据模型
   - 添加必要的依赖项
   - 更新用户设置模型

2. **阶段二：核心实现 (2025-04-04 - 2025-04-10)**
   - 实现生物识别服务
   - 实现TOTP服务
   - 实现多因素认证管理器

3. **阶段三：UI集成和测试 (2025-04-11 - 2025-04-17)**
   - 创建安全设置界面
   - 集成认证流程
   - 测试和验证
   - 解决问题和优化

## 安全考虑 / Security Considerations

1. **密钥存储 / Key Storage**
   - TOTP密钥必须使用强加密存储
   - 生物识别数据由操作系统安全存储

2. **回退机制 / Fallback Mechanisms**
   - 提供备用认证方法，防止用户被锁定
   - 实现安全的账户恢复流程

3. **防暴力破解 / Anti-Brute Force**
   - 实现尝试次数限制
   - 添加延迟机制

4. **用户教育 / User Education**
   - 提供清晰的设置指南
   - 解释多因素认证的重要性

## 兼容性考虑 / Compatibility Considerations

1. **设备支持 / Device Support**
   - 处理不支持生物识别的设备
   - 确保在所有Windows版本上的兼容性

2. **回退选项 / Fallback Options**
   - 为不支持Windows Hello的设备提供替代方案
   - 确保TOTP作为通用选项可用

## 维护计划 / Maintenance Plan

1. **定期安全审查 / Regular Security Reviews**
   - 定期评估认证机制的安全性
   - 跟踪新的安全威胁和最佳实践

2. **用户反馈收集 / User Feedback Collection**
   - 收集用户对认证流程的反馈
   - 根据用户体验持续优化

3. **文档更新 / Documentation Updates**
   - 保持安全实施文档的最新
   - 更新用户指南以反映新的安全功能 