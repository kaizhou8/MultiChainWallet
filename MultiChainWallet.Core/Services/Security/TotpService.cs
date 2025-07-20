using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Interfaces;
using OtpNet;

namespace MultiChainWallet.Core.Services.Security
{
    /// <summary>
    /// TOTP服务实现
    /// TOTP service implementation
    /// </summary>
    public class TotpService : ITotpService
    {
        private readonly ISecurityService _securityService;
        private readonly ILogger<TotpService> _logger;
        private const int DefaultTimeStep = 30;
        private const int DefaultCodeSize = 6;

        public TotpService(
            ISecurityService securityService,
            ILogger<TotpService> logger)
        {
            _securityService = securityService;
            _logger = logger;
        }

        /// <inheritdoc/>
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
                
                return base32Secret;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成TOTP密钥时出错 / Error generating TOTP key");
                throw;
            }
        }

        /// <inheritdoc/>
        public string GenerateQrCodeUri(string secretKey, string accountName, string issuer)
        {
            try
            {
                // 生成用于扫描的二维码URI
                // Generate QR code URI for scanning
                return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(accountName)}?" +
                       $"secret={secretKey}&issuer={Uri.EscapeDataString(issuer)}" +
                       $"&algorithm=SHA1&digits={DefaultCodeSize}&period={DefaultTimeStep}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成QR码URI时出错 / Error generating QR code URI");
                throw;
            }
        }

        /// <inheritdoc/>
        public bool VerifyCode(string secretKey, string code)
        {
            try
            {
                // 解码密钥
                // Decode the key
                byte[] key = Base32Encoding.ToBytes(secretKey);
                
                // 创建TOTP对象
                // Create TOTP object
                var totp = new Totp(key, DefaultTimeStep, OtpHashMode.Sha1, DefaultCodeSize);
                
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

        /// <inheritdoc/>
        public string GetCurrentCode(string secretKey)
        {
            try
            {
                // 解码密钥
                // Decode the key
                byte[] key = Base32Encoding.ToBytes(secretKey);
                
                // 创建TOTP对象
                // Create TOTP object
                var totp = new Totp(key, DefaultTimeStep, OtpHashMode.Sha1, DefaultCodeSize);
                
                // 获取当前代码
                // Get current code
                return totp.ComputeTotp();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取当前TOTP代码时出错 / Error getting current TOTP code");
                throw;
            }
        }
    }
} 