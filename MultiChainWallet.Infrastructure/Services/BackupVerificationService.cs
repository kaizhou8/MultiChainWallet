using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using MultiChainWallet.Core.Models;
using MultiChainWallet.Core.Interfaces;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 备份验证服务
    /// Backup verification service
    /// </summary>
    public class BackupVerificationService
    {
        private readonly IPerformanceMonitorService _performanceMonitor;

        public BackupVerificationService(IPerformanceMonitorService performanceMonitor)
        {
            _performanceMonitor = performanceMonitor;
        }

        /// <summary>
        /// 验证备份数据
        /// Verify backup data
        /// </summary>
        public async Task<BackupVerificationResult> VerifyBackupDataAsync(MultiChainWallet.Core.Models.BackupData backupData)
        {
            if (backupData == null)
            {
                throw new ArgumentNullException(nameof(backupData));
            }

            return await _performanceMonitor.MonitorOperationAsync(
                "VerifyBackupData",
                async () =>
                {
                    var errors = new List<string>();

                    // Verify wallet accounts
                    if (backupData.WalletAccounts == null)
                    {
                        errors.Add("Wallet accounts list cannot be null");
                    }
                    else
                    {
                        foreach (var wallet in backupData.WalletAccounts)
                        {
                            if (string.IsNullOrEmpty(wallet.Address))
                            {
                                errors.Add("Invalid wallet address");
                            }
                            if (string.IsNullOrEmpty(wallet.EncryptedPrivateKey))
                            {
                                errors.Add("Invalid wallet encrypted private key");
                            }
                        }
                    }

                    // Verify token balances
                    if (backupData.TokenBalances == null)
                    {
                        errors.Add("Token balances list cannot be null");
                    }

                    // Verify transactions
                    if (backupData.Transactions == null)
                    {
                        errors.Add("Transactions list cannot be null");
                    }

                    // Verify metadata
                    if (backupData.CreatedAt == DateTime.MinValue)
                    {
                        errors.Add("Invalid creation date");
                    }
                    if (backupData.CreatedAt > DateTime.UtcNow)
                    {
                        errors.Add("Creation date cannot be a future date");
                    }
                    if (string.IsNullOrEmpty(backupData.Version))
                    {
                        errors.Add("Version cannot be empty");
                    }
                    if (string.IsNullOrEmpty((backupData as dynamic).Checksum))
                    {
                        errors.Add("Checksum cannot be empty");
                    }

                    return new BackupVerificationResult
                    {
                        IsValid = errors.Count == 0,
                        ValidationErrors = errors
                    };
                });
        }
    }

    /// <summary>
    /// 备份验证结果
    /// Backup verification result
    /// </summary>
    public class BackupVerificationResult
    {
        /// <summary>
        /// 是否有效
        /// Is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 验证错误列表
        /// List of validation errors
        /// </summary>
        public List<string> ValidationErrors { get; set; } = new List<string>();
    }
}
