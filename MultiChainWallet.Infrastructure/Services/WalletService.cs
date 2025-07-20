using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Models;
using MultiChainWallet.Core.Services;
using MultiChainWallet.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 钱包服务实现
    /// Wallet service implementation
    /// </summary>
    public class WalletService : IWalletService
    {
        private readonly WalletDbContext _dbContext;
        private readonly EthereumWallet _ethereumWallet;
        private readonly BitcoinWallet _bitcoinWallet;
        private readonly string _encryptionKey;
        private readonly ILogger _logger;
        private readonly IWalletRepository _walletRepository;
        private readonly ICryptoService _cryptoService;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="dbContext">数据库上下文 / Database context</param>
        /// <param name="ethereumWallet">以太坊钱包 / Ethereum wallet</param>
        /// <param name="bitcoinWallet">比特币钱包 / Bitcoin wallet</param>
        /// <param name="encryptionKey">加密密钥 / Encryption key</param>
        /// <param name="logger">日志记录器 / Logger</param>
        /// <param name="walletRepository">钱包仓库 / Wallet repository</param>
        /// <param name="cryptoService">加密服务 / Crypto service</param>
        public WalletService(
            WalletDbContext dbContext, 
            EthereumWallet ethereumWallet,
            BitcoinWallet bitcoinWallet,
            string encryptionKey,
            ILogger logger,
            IWalletRepository walletRepository,
            ICryptoService cryptoService)
        {
            _dbContext = dbContext;
            _ethereumWallet = ethereumWallet;
            _bitcoinWallet = bitcoinWallet;
            _encryptionKey = encryptionKey;
            _logger = logger;
            _walletRepository = walletRepository;
            _cryptoService = cryptoService;
        }

        /// <summary>
        /// 创建新钱包
        /// Create a new wallet
        /// </summary>
        /// <param name="chainType">链类型 / Chain type</param>
        /// <param name="password">密码 / Password</param>
        /// <returns>钱包账户 / Wallet account</returns>
        public async Task<WalletAccount> CreateWalletAsync(ChainType chainType, string password)
        {
            string address;
            string name;

            switch (chainType)
            {
                case ChainType.Ethereum:
                    address = await _ethereumWallet.CreateWalletAsync();
                    name = $"ETH Wallet {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    break;

                case ChainType.Bitcoin:
                    address = await _bitcoinWallet.CreateWalletAsync();
                    name = $"BTC Wallet {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    break;

                default:
                    throw new ArgumentException("不支持的链类型 / Unsupported chain type", nameof(chainType));
            }

            var account = new WalletAccount
            {
                Address = address,
                ChainType = chainType,
                Name = name,
                EncryptedPrivateKey = EncryptPrivateKey(password)
            };

            _dbContext.WalletAccounts.Add(account);
            await _dbContext.SaveChangesAsync();
            return account;
        }

        /// <summary>
        /// 获取所有钱包
        /// Get all wallets
        /// </summary>
        /// <returns>钱包列表 / Wallet list</returns>
        public async Task<IEnumerable<WalletAccount>> GetWalletsAsync()
        {
            return await _dbContext.WalletAccounts.ToListAsync();
        }

        /// <summary>
        /// 获取钱包余额
        /// Get wallet balance
        /// </summary>
        /// <param name="address">钱包地址 / Wallet address</param>
        /// <param name="chainType">链类型 / Chain type</param>
        /// <returns>余额 / Balance</returns>
        public async Task<decimal> GetBalanceAsync(string address, ChainType chainType)
        {
            switch (chainType)
            {
                case ChainType.Ethereum:
                    return await _ethereumWallet.GetBalanceAsync(address);

                case ChainType.Bitcoin:
                    return await _bitcoinWallet.GetBalanceAsync(address);

                default:
                    throw new ArgumentException("不支持的链类型 / Unsupported chain type", nameof(chainType));
            }
        }

        /// <summary>
        /// 发送交易
        /// Send transaction
        /// </summary>
        /// <param name="fromAddress">发送地址 / From address</param>
        /// <param name="toAddress">接收地址 / To address</param>
        /// <param name="amount">金额 / Amount</param>
        /// <param name="password">密码 / Password</param>
        /// <param name="chainType">链类型 / Chain type</param>
        /// <returns>交易哈希 / Transaction hash</returns>
        public async Task<string> SendTransactionAsync(
            string fromAddress, 
            string toAddress, 
            decimal amount, 
            string password, 
            ChainType chainType)
        {
            var account = await _dbContext.WalletAccounts.FindAsync(fromAddress);
            if (account == null)
                throw new ArgumentException("钱包不存在 / Wallet does not exist", nameof(fromAddress));

            var privateKey = DecryptPrivateKey(account.EncryptedPrivateKey, password);

            switch (chainType)
            {
                case ChainType.Ethereum:
                    return await _ethereumWallet.SendTransactionAsync(fromAddress, toAddress, amount);

                case ChainType.Bitcoin:
                    return await _bitcoinWallet.SendTransactionAsync(fromAddress, toAddress, amount);

                default:
                    throw new ArgumentException("不支持的链类型 / Unsupported chain type", nameof(chainType));
            }
        }

        /// <summary>
        /// 验证密码
        /// Validate password
        /// </summary>
        /// <param name="password">密码 / Password</param>
        /// <returns>是否有效 / Is valid</returns>
        public async Task<bool> ValidatePasswordAsync(string password)
        {
            try
            {
                // 从数据库获取当前钱包的密码哈希
                // Get current wallet's password hash from database
                var wallet = await _walletRepository.GetCurrentWalletAsync();
                if (wallet == null)
                {
                    return false;
                }

                // 验证密码
                // Validate password
                var passwordHash = _cryptoService.HashPassword(password);
                return passwordHash == wallet.PasswordHash;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证密码失败 / Failed to validate password");
                throw;
            }
        }

        /// <summary>
        /// 修改密码
        /// Change password
        /// </summary>
        /// <param name="oldPassword">旧密码 / Old password</param>
        /// <param name="newPassword">新密码 / New password</param>
        /// <returns>是否成功 / Success</returns>
        public async Task<bool> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            try
            {
                // 验证旧密码
                // Validate old password
                var isValid = await ValidatePasswordAsync(oldPassword);
                if (!isValid)
                {
                    return false;
                }

                // 获取当前钱包
                // Get current wallet
                var wallet = await _walletRepository.GetCurrentWalletAsync();
                if (wallet == null)
                {
                    return false;
                }

                // 更新密码哈希
                // Update password hash
                wallet.PasswordHash = _cryptoService.HashPassword(newPassword);
                await _walletRepository.UpdateWalletAsync(wallet);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改密码失败 / Failed to change password");
                throw;
            }
        }

        /// <summary>
        /// 备份钱包
        /// Backup wallet
        /// </summary>
        /// <param name="backupPath">备份路径 / Backup path</param>
        /// <param name="password">密码 / Password</param>
        /// <returns>是否成功 / Success</returns>
        public async Task<bool> BackupWalletAsync(string backupPath, string password)
        {
            try
            {
                var wallets = await GetWalletsAsync();
                var backupData = JsonSerializer.Serialize(wallets);
                var encryptedData = _cryptoService.Encrypt(backupData, password);
                await File.WriteAllTextAsync(backupPath, encryptedData);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "备份钱包失败 / Failed to backup wallet");
                return false;
            }
        }

        /// <summary>
        /// 按组获取钱包
        /// Get wallets by group
        /// </summary>
        /// <param name="group">组名 / Group name</param>
        /// <returns>钱包列表 / Wallet list</returns>
        public async Task<IEnumerable<WalletAccount>> GetWalletsByGroupAsync(string group)
        {
            return await _walletRepository.GetWalletsByGroupAsync(group);
        }

        /// <summary>
        /// 获取所有钱包组
        /// Get all wallet groups
        /// </summary>
        /// <returns>组名列表 / Group name list</returns>
        public async Task<IEnumerable<string>> GetAllGroupsAsync()
        {
            return await _walletRepository.GetAllGroupsAsync();
        }

        /// <summary>
        /// 按标签搜索钱包
        /// Search wallets by tag
        /// </summary>
        /// <param name="tag">标签 / Tag</param>
        /// <returns>钱包列表 / Wallet list</returns>
        public async Task<IEnumerable<WalletAccount>> SearchWalletsByTagAsync(string tag)
        {
            return await _walletRepository.SearchWalletsByTagAsync(tag);
        }

        /// <summary>
        /// 获取所有钱包标签
        /// Get all wallet tags
        /// </summary>
        /// <returns>标签列表 / Tag list</returns>
        public async Task<IEnumerable<string>> GetAllTagsAsync()
        {
            return await _walletRepository.GetAllTagsAsync();
        }

        /// <summary>
        /// 更新钱包组
        /// Update wallet group
        /// </summary>
        /// <param name="address">钱包地址 / Wallet address</param>
        /// <param name="group">组名 / Group name</param>
        /// <returns>是否成功 / Success</returns>
        public async Task<bool> UpdateWalletGroupAsync(string address, string group)
        {
            try
            {
                var wallet = await _dbContext.WalletAccounts.FindAsync(address);
                if (wallet == null)
                {
                    return false;
                }
                
                return await _walletRepository.UpdateWalletGroupAsync(address, group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新钱包组失败 / Failed to update wallet group");
                return false;
            }
        }

        /// <summary>
        /// 更新钱包标签
        /// Update wallet tags
        /// </summary>
        /// <param name="address">钱包地址 / Wallet address</param>
        /// <param name="tags">标签列表（逗号分隔） / Tag list (comma separated)</param>
        /// <returns>是否成功 / Success</returns>
        public async Task<bool> UpdateWalletTagsAsync(string address, string tags)
        {
            try
            {
                var wallet = await _dbContext.WalletAccounts.FindAsync(address);
                if (wallet == null)
                {
                    return false;
                }
                
                return await _walletRepository.UpdateWalletTagsAsync(address, tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新钱包标签失败 / Failed to update wallet tags");
                return false;
            }
        }

        /// <summary>
        /// 导出钱包到JSON文件
        /// Export wallets to JSON file
        /// </summary>
        /// <param name="filePath">文件路径 / File path</param>
        /// <returns>是否成功 / Success</returns>
        public async Task<bool> ExportWalletsToJsonAsync(string filePath)
        {
            try
            {
                var wallets = await GetWalletsAsync();
                var exportData = wallets.Select(w => new
                {
                    w.Address,
                    w.Name,
                    w.ChainType,
                    w.Group,
                    Tags = w.Tags,
                    w.CreatedAt,
                    w.LastUsedAt,
                    w.UsageCount
                });
                
                var jsonData = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                await File.WriteAllTextAsync(filePath, jsonData);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出钱包失败 / Failed to export wallets");
                return false;
            }
        }

        /// <summary>
        /// 从JSON文件导入钱包
        /// Import wallets from JSON file
        /// </summary>
        /// <param name="filePath">文件路径 / File path</param>
        /// <param name="overwrite">是否覆盖现有钱包 / Whether to overwrite existing wallets</param>
        /// <returns>导入的钱包数量 / Number of imported wallets</returns>
        public async Task<int> ImportWalletsFromJsonAsync(string filePath, bool overwrite = false)
        {
            try
            {
                var jsonData = await File.ReadAllTextAsync(filePath);
                return await _walletRepository.ImportWalletsFromJsonAsync(jsonData, overwrite);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入钱包失败 / Failed to import wallets");
                return 0;
            }
        }

        /// <summary>
        /// 更新钱包使用统计
        /// Update wallet usage statistics
        /// </summary>
        /// <param name="address">钱包地址 / Wallet address</param>
        /// <returns>是否成功 / Success</returns>
        public async Task<bool> UpdateWalletUsageStatsAsync(string address)
        {
            try
            {
                return await _walletRepository.UpdateWalletUsageStatsAsync(address);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新钱包使用统计失败 / Failed to update wallet usage statistics");
                return false;
            }
        }

        // 保持现有的加密和解密方法不变
        // Keep existing encryption and decryption methods unchanged
        private string EncryptPrivateKey(string privateKey)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32, '0'));
                aes.IV = new byte[16];

                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                var plainBytes = Encoding.UTF8.GetBytes(privateKey);

                using (var msEncrypt = new System.IO.MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(plainBytes, 0, plainBytes.Length);
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        private string DecryptPrivateKey(string encryptedPrivateKey, string password)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32, '0'));
                aes.IV = new byte[16];

                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                var cipherBytes = Convert.FromBase64String(encryptedPrivateKey);

                using (var msDecrypt = new System.IO.MemoryStream(cipherBytes))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }
    }
}
