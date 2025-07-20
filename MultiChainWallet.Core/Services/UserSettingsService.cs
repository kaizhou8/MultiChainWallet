using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Interfaces;
using MultiChainWallet.Core.Models;

namespace MultiChainWallet.Core.Services
{
    /// <summary>
    /// 用户设置服务实现
    /// User settings service implementation
    /// </summary>
    public class UserSettingsService : IUserSettingsService
    {
        private readonly string _settingsDirectory;
        private readonly string _securitySettingsPath;
        private readonly string _passwordHashPath;
        private readonly ISecurityService _securityService;
        private readonly ILogger<UserSettingsService> _logger;
        private SecuritySettings _cachedSettings;
        private string _cachedPasswordHash;
        private readonly object _lock = new object();

        public UserSettingsService(
            ISecurityService securityService,
            ILogger<UserSettingsService> logger)
        {
            _securityService = securityService;
            _logger = logger;

            // 设置文件路径
            // Set file paths
            _settingsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MultiChainWallet",
                "Settings");
            _securitySettingsPath = Path.Combine(_settingsDirectory, "security.dat");
            _passwordHashPath = Path.Combine(_settingsDirectory, "pwd.dat");

            // 确保目录存在
            // Ensure directory exists
            Directory.CreateDirectory(_settingsDirectory);
        }

        /// <inheritdoc/>
        public async Task<SecuritySettings> GetSecuritySettingsAsync()
        {
            try
            {
                // 检查缓存
                // Check cache
                if (_cachedSettings != null)
                {
                    return _cachedSettings;
                }

                // 如果文件不存在，返回默认设置
                // If file doesn't exist, return default settings
                if (!File.Exists(_securitySettingsPath))
                {
                    _cachedSettings = new SecuritySettings
                    {
                        LastUpdated = DateTime.UtcNow
                    };
                    return _cachedSettings;
                }

                // 读取并解密文件
                // Read and decrypt file
                byte[] encryptedData = await File.ReadAllBytesAsync(_securitySettingsPath);
                byte[] decryptedData = await _securityService.DecryptAsync(
                    encryptedData,
                    await GetPasswordHashAsync());

                // 反序列化设置
                // Deserialize settings
                string json = System.Text.Encoding.UTF8.GetString(decryptedData);
                _cachedSettings = JsonSerializer.Deserialize<SecuritySettings>(json);
                return _cachedSettings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取安全设置时出错 / Error getting security settings");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task SaveSecuritySettingsAsync(SecuritySettings settings)
        {
            try
            {
                // 更新时间戳
                // Update timestamp
                settings.LastUpdated = DateTime.UtcNow;

                // 序列化设置
                // Serialize settings
                string json = JsonSerializer.Serialize(settings);
                byte[] data = System.Text.Encoding.UTF8.GetBytes(json);

                // 加密数据
                // Encrypt data
                byte[] encryptedData = await _securityService.EncryptAsync(
                    data,
                    await GetPasswordHashAsync());

                // 保存到文件
                // Save to file
                await File.WriteAllBytesAsync(_securitySettingsPath, encryptedData);

                // 更新缓存
                // Update cache
                _cachedSettings = settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存安全设置时出错 / Error saving security settings");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetPasswordHashAsync()
        {
            try
            {
                // 检查缓存
                // Check cache
                if (!string.IsNullOrEmpty(_cachedPasswordHash))
                {
                    return _cachedPasswordHash;
                }

                // 如果文件不存在，抛出异常
                // If file doesn't exist, throw exception
                if (!File.Exists(_passwordHashPath))
                {
                    throw new InvalidOperationException("未设置密码 / Password not set");
                }

                // 读取并解密文件
                // Read and decrypt file
                byte[] encryptedData = await File.ReadAllBytesAsync(_passwordHashPath);
                byte[] decryptedData = await _securityService.DecryptAsync(
                    encryptedData,
                    _securityService.GenerateSessionToken());

                // 更新缓存
                // Update cache
                _cachedPasswordHash = System.Text.Encoding.UTF8.GetString(decryptedData);
                return _cachedPasswordHash;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取密码哈希时出错 / Error getting password hash");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task SavePasswordHashAsync(string passwordHash)
        {
            try
            {
                // 加密密码哈希
                // Encrypt password hash
                byte[] data = System.Text.Encoding.UTF8.GetBytes(passwordHash);
                byte[] encryptedData = await _securityService.EncryptAsync(
                    data,
                    _securityService.GenerateSessionToken());

                // 保存到文件
                // Save to file
                await File.WriteAllBytesAsync(_passwordHashPath, encryptedData);

                // 更新缓存
                // Update cache
                _cachedPasswordHash = passwordHash;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存密码哈希时出错 / Error saving password hash");
                throw;
            }
        }
    }
} 