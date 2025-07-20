using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using MultiChainWallet.Core.Models;
using MultiChainWallet.Core.Interfaces;
using MultiChainWallet.Infrastructure.Data;
using System.IO.Compression;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 备份服务类
    /// Backup service class
    /// </summary>
    public class BackupService : IBackupService
    {
        private readonly IWalletRepository _walletRepository;
        private readonly ITokenBalanceRepository _tokenBalanceRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ISecurityService _securityService;
        private readonly ICompressionService _compressionService;
        private readonly IBackupVerificationService _backupVerificationService;

        public BackupService(
            IWalletRepository walletRepository,
            ITokenBalanceRepository tokenBalanceRepository,
            ITransactionRepository transactionRepository,
            ISecurityService securityService,
            ICompressionService compressionService,
            IBackupVerificationService backupVerificationService)
        {
            _walletRepository = walletRepository;
            _tokenBalanceRepository = tokenBalanceRepository;
            _transactionRepository = transactionRepository;
            _securityService = securityService;
            _compressionService = compressionService;
            _backupVerificationService = backupVerificationService;
        }

        /// <summary>
        /// 创建备份
        /// Create backup
        /// </summary>
        public async Task<string> CreateBackupAsync(string password)
        {
            try
            {
                // 获取所有钱包数据 / Get all wallet data
                var wallets = await _walletRepository.GetAllAsync();
                var backupData = new BackupData
                {
                    Version = "1.0.0",
                    CreatedAt = DateTime.UtcNow,
                    WalletAccounts = wallets,
                    TokenBalances = new List<TokenBalance>(),
                    Transactions = new List<Transaction>()
                };

                // 获取每个钱包的代币余额和交易记录 / Get token balances and transactions for each wallet
                foreach (var wallet in wallets)
                {
                    var tokenBalances = await _tokenBalanceRepository.GetAllForWalletAsync(wallet.Address);
                    var transactions = await _transactionRepository.GetAllForWalletAsync(wallet.Address);
                    ((List<TokenBalance>)backupData.TokenBalances).AddRange(tokenBalances);
                    ((List<Transaction>)backupData.Transactions).AddRange(transactions);
                }

                // 序列化数据 / Serialize data
                var jsonData = JsonSerializer.Serialize(backupData);
                var dataBytes = System.Text.Encoding.UTF8.GetBytes(jsonData);

                // 压缩数据 / Compress data
                var compressedData = await _compressionService.CompressAsync(dataBytes);

                // 加密数据 / Encrypt data
                var encryptedData = await _securityService.EncryptAsync(compressedData, password);

                // 保存到文件 / Save to file
                var backupPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    $"MultiChainWallet_Backup_{DateTime.Now:yyyyMMddHHmmss}.bak"
                );
                await File.WriteAllBytesAsync(backupPath, encryptedData);

                return backupPath;
            }
            catch (Exception ex)
            {
                throw new BackupException("Failed to create backup", ex);
            }
        }

        /// <summary>
        /// 从备份恢复
        /// Restore from backup
        /// </summary>
        public async Task RestoreFromBackupAsync(string backupPath, string password)
        {
            try
            {
                // 验证备份 / Verify backup
                if (!await VerifyBackupAsync(backupPath))
                {
                    throw new BackupException("Backup verification failed");
                }

                // 读取备份文件 / Read backup file
                var encryptedData = await File.ReadAllBytesAsync(backupPath);

                // 解密数据 / Decrypt data
                var decryptedData = await _securityService.DecryptAsync(encryptedData, password);

                // 解压数据 / Decompress data
                var decompressedData = await _compressionService.DecompressAsync(decryptedData);

                // 反序列化数据 / Deserialize data
                var jsonData = System.Text.Encoding.UTF8.GetString(decompressedData);
                var backupData = JsonSerializer.Deserialize<BackupData>(jsonData);

                // 恢复钱包数据 / Restore wallet data
                foreach (var wallet in backupData.WalletAccounts)
                {
                    await _walletRepository.AddAsync(wallet);
                }

                // 恢复代币余额 / Restore token balances
                foreach (var balance in backupData.TokenBalances)
                {
                    await _tokenBalanceRepository.AddAsync(balance);
                }

                // 恢复交易记录 / Restore transactions
                foreach (var transaction in backupData.Transactions)
                {
                    await _transactionRepository.AddAsync(transaction);
                }
            }
            catch (Exception ex)
            {
                throw new BackupException("Failed to restore from backup", ex);
            }
        }

        /// <summary>
        /// 验证备份
        /// Verify backup
        /// </summary>
        public async Task<bool> VerifyBackupAsync(string backupPath)
        {
            try
            {
                // 验证文件是否存在 / Verify file exists
                if (!File.Exists(backupPath))
                {
                    return false;
                }

                // 验证数据完整性 / Verify data integrity
                var dataIntegrityValid = await _backupVerificationService.VerifyDataIntegrityAsync(backupPath);
                if (!dataIntegrityValid)
                {
                    return false;
                }

                // 验证数据结构 / Verify data structure
                var dataStructureValid = await _backupVerificationService.VerifyDataStructureAsync(backupPath);
                if (!dataStructureValid)
                {
                    return false;
                }

                // 验证版本兼容性 / Verify version compatibility
                var versionCompatible = await _backupVerificationService.VerifyVersionCompatibilityAsync(backupPath);
                if (!versionCompatible)
                {
                    return false;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 备份数据类
    /// Backup data class
    /// </summary>
    public class BackupData
    {
        public string Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public IEnumerable<WalletAccount> WalletAccounts { get; set; }
        public IEnumerable<TokenBalance> TokenBalances { get; set; }
        public IEnumerable<Transaction> Transactions { get; set; }
    }

    /// <summary>
    /// 备份异常类
    /// Backup exception class
    /// </summary>
    public class BackupException : Exception
    {
        public BackupException(string message) : base(message)
        {
        }

        public BackupException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
