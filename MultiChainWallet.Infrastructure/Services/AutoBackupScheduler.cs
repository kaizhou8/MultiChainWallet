using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using MultiChainWallet.Core.Interfaces;
using System.Runtime.Versioning;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 自动备份调度器
    /// Auto backup scheduler
    /// </summary>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    public class AutoBackupScheduler : BackgroundService
    {
        private readonly ILogger<AutoBackupScheduler> _logger;
        private readonly IBackupService _backupService;
        private readonly ISecurityService _securityService;
        private Timer _backupTimer;
        private const string BACKUP_CONFIG_FILE = "backup_config.json";

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public AutoBackupScheduler(
            ILogger<AutoBackupScheduler> logger,
            IBackupService backupService,
            ISecurityService securityService)
        {
            _logger = logger;
            _backupService = backupService;
            _securityService = securityService;
        }

        /// <summary>
        /// 启动自动备份
        /// Start auto backup
        /// </summary>
        public async Task StartAsync(string password, TimeSpan interval)
        {
            try
            {
                // 验证密码强度
                // Validate password strength
                var result = _securityService.ValidatePasswordStrength(password);
                if (!result.IsValid)
                {
                    throw new ArgumentException(result.ErrorMessage);
                }

                // 保存加密的密码
                // Save encrypted password
                await SaveEncryptedPasswordAsync(password);

                // 启动定时器
                // Start timer
                _backupTimer = new Timer(
                    async _ => await CreateBackupAsync(),
                    null,
                    TimeSpan.Zero,
                    interval);

                _logger.LogInformation(
                    "自动备份已启动，间隔时间: {Interval} / Auto backup started, interval: {Interval}",
                    interval);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动自动备份失败 / Failed to start auto backup");
                throw;
            }
        }

        /// <summary>
        /// 停止自动备份
        /// Stop auto backup
        /// </summary>
        public void Stop()
        {
            _backupTimer?.Dispose();
            _backupTimer = null;
            _logger.LogInformation("自动备份已停止 / Auto backup stopped");
        }

        /// <summary>
        /// 创建备份
        /// Create backup
        /// </summary>
        private async Task CreateBackupAsync()
        {
            try
            {
                var password = await LoadEncryptedPasswordAsync();
                if (string.IsNullOrEmpty(password))
                {
                    _logger.LogError("找不到加密的密码 / Encrypted password not found");
                    return;
                }

                await _backupService.CreateBackupAsync(password);
                _logger.LogInformation("自动备份已完成 / Auto backup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动备份失败 / Auto backup failed");
            }
        }

        /// <summary>
        /// 保存加密的密码
        /// Save encrypted password
        /// </summary>
        private async Task SaveEncryptedPasswordAsync(string password)
        {
            var config = new BackupConfig
            {
                EncryptedPassword = await Task.Run(() =>
                {
                    var entropy = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());
                    var passwordBytes = Encoding.UTF8.GetBytes(password);
                    return Convert.ToBase64String(
                        ProtectedData.Protect(
                            passwordBytes,
                            entropy,
                            DataProtectionScope.CurrentUser
                        )
                    );
                })
            };

            var json = JsonSerializer.Serialize(config);
            await File.WriteAllTextAsync(BACKUP_CONFIG_FILE, json);
        }

        /// <summary>
        /// 加载加密的密码
        /// Load encrypted password
        /// </summary>
        private async Task<string> LoadEncryptedPasswordAsync()
        {
            if (!File.Exists(BACKUP_CONFIG_FILE))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(BACKUP_CONFIG_FILE);
            var config = JsonSerializer.Deserialize<BackupConfig>(json);

            return await Task.Run(() =>
            {
                var encryptedBytes = Convert.FromBase64String(config.EncryptedPassword);
                var entropy = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());
                var passwordBytes = ProtectedData.Unprotect(
                    encryptedBytes,
                    entropy,
                    DataProtectionScope.CurrentUser
                );
                return Encoding.UTF8.GetString(passwordBytes);
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await StartAsync("password", TimeSpan.FromHours(1));
        }
    }

    /// <summary>
    /// 备份配置
    /// Backup configuration
    /// </summary>
    public class BackupConfig
    {
        /// <summary>
        /// 加密的密码
        /// Encrypted password
        /// </summary>
        public string EncryptedPassword { get; set; }
    }
}
