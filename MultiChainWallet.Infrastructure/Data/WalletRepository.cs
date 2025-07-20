using MultiChainWallet.Core.Interfaces;
using MultiChainWallet.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;

namespace MultiChainWallet.Infrastructure.Data
{
    /// <summary>
    /// 钱包仓储类
    /// Wallet repository class
    /// </summary>
    public class WalletRepository : BaseRepository, IWalletRepository
    {
        /// <summary>
        /// 默认构造函数
        /// Default constructor
        /// </summary>
        public WalletRepository() : base()
        {
        }

        /// <summary>
        /// 带连接字符串的构造函数（用于测试）
        /// Constructor with connection string (for testing)
        /// </summary>
        /// <param name="connectionString">数据库连接字符串 / Database connection string</param>
        public WalletRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// 获取所有钱包账户
        /// Get all wallet accounts
        /// </summary>
        public async Task<IEnumerable<WalletAccount>> GetAllAsync()
        {
            const string sql = "SELECT * FROM WalletAccounts";
            return await QueryAsync<WalletAccount>(sql);
        }

        /// <summary>
        /// 根据地址获取钱包账户
        /// Get wallet account by address
        /// </summary>
        public async Task<WalletAccount> GetByAddressAsync(string address)
        {
            const string sql = "SELECT * FROM WalletAccounts WHERE Address = @Address";
            return await QuerySingleOrDefaultAsync<WalletAccount>(sql, new { Address = address });
        }

        /// <summary>
        /// 添加钱包账户
        /// Add wallet account
        /// </summary>
        public async Task AddAsync(WalletAccount wallet)
        {
            const string sql = @"
                INSERT INTO WalletAccounts (
                    Address, EncryptedPrivateKey, ChainType, Name, PasswordHash, IsCurrent,
                    Group, Tags, Metadata, CreatedAt, LastUsedAt, UsageCount
                )
                VALUES (
                    @Address, @EncryptedPrivateKey, @ChainType, @Name, @PasswordHash, @IsCurrent,
                    @Group, @Tags, @Metadata, @CreatedAt, @LastUsedAt, @UsageCount
                )";
            await ExecuteAsync(sql, wallet);
        }

        /// <summary>
        /// 批量添加钱包账户
        /// Batch add wallet accounts
        /// </summary>
        /// <param name="wallets">钱包账户列表 / List of wallet accounts</param>
        public async Task AddBatchAsync(IEnumerable<WalletAccount> wallets)
        {
            await ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                const string sql = @"
                    INSERT INTO WalletAccounts (
                        Address, EncryptedPrivateKey, ChainType, Name, PasswordHash, IsCurrent,
                        Group, Tags, Metadata, CreatedAt, LastUsedAt, UsageCount
                    )
                    VALUES (
                        @Address, @EncryptedPrivateKey, @ChainType, @Name, @PasswordHash, @IsCurrent,
                        @Group, @Tags, @Metadata, @CreatedAt, @LastUsedAt, @UsageCount
                    )";
                
                foreach (var wallet in wallets)
                {
                    await connection.ExecuteAsync(sql, wallet, transaction: transaction);
                }
            });
        }

        /// <summary>
        /// 根据链类型获取钱包账户
        /// Get wallet accounts by chain type
        /// </summary>
        /// <param name="chainType">链类型 / Chain type</param>
        public async Task<IEnumerable<WalletAccount>> GetByChainTypeAsync(string chainType)
        {
            const string sql = "SELECT * FROM WalletAccounts WHERE ChainType = @ChainType";
            return await QueryAsync<WalletAccount>(sql, new { ChainType = chainType });
        }

        /// <summary>
        /// 获取钱包总数
        /// Get total wallet count
        /// </summary>
        public async Task<int> GetTotalCountAsync()
        {
            const string sql = "SELECT COUNT(*) FROM WalletAccounts";
            return await QuerySingleOrDefaultAsync<int>(sql);
        }

        /// <summary>
        /// 备份钱包数据
        /// Backup wallet data
        /// </summary>
        /// <param name="backupPath">备份文件路径 / Backup file path</param>
        public async Task BackupAsync(string backupPath)
        {
            var wallets = await GetAllAsync();
            
            // 使用事务确保备份的一致性
            // Use transaction to ensure backup consistency
            await ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                using (var backupConnection = new SqliteConnection($"Data Source={backupPath}"))
                {
                    await backupConnection.OpenAsync();
                    
                    // 创建备份表
                    // Create backup table
                    var createTableSql = @"
                        CREATE TABLE IF NOT EXISTS WalletAccounts (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Address TEXT NOT NULL UNIQUE,
                            EncryptedPrivateKey TEXT NOT NULL,
                            ChainType TEXT NOT NULL,
                            Name TEXT,
                            PasswordHash TEXT,
                            IsCurrent INTEGER NOT NULL DEFAULT 0,
                            BackupDate TEXT NOT NULL
                        )";
                    
                    await backupConnection.ExecuteAsync(createTableSql);
                    
                    // 插入钱包数据
                    // Insert wallet data
                    var insertSql = @"
                        INSERT OR REPLACE INTO WalletAccounts 
                        (Address, EncryptedPrivateKey, ChainType, Name, PasswordHash, IsCurrent, BackupDate)
                        VALUES (@Address, @EncryptedPrivateKey, @ChainType, @Name, @PasswordHash, @IsCurrent, @BackupDate)";
                    
                    foreach (var wallet in wallets)
                    {
                        await backupConnection.ExecuteAsync(insertSql, new
                        {
                            wallet.Address,
                            wallet.EncryptedPrivateKey,
                            wallet.ChainType,
                            wallet.Name,
                            wallet.PasswordHash,
                            wallet.IsCurrent,
                            BackupDate = DateTime.UtcNow.ToString("o")
                        });
                    }
                }
            });
        }

        /// <summary>
        /// 从备份恢复钱包数据
        /// Restore wallet data from backup
        /// </summary>
        /// <param name="backupPath">备份文件路径 / Backup file path</param>
        /// <param name="overwrite">是否覆盖现有数据 / Whether to overwrite existing data</param>
        public async Task RestoreAsync(string backupPath, bool overwrite = false)
        {
            // 使用事务确保恢复的一致性
            // Use transaction to ensure restore consistency
            await ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                using (var backupConnection = new SqliteConnection($"Data Source={backupPath}"))
                {
                    await backupConnection.OpenAsync();
                    
                    // 获取备份的钱包数据
                    // Get backed up wallet data
                    var backupWallets = await backupConnection.QueryAsync<WalletAccount>("SELECT * FROM WalletAccounts");
                    
                    if (overwrite)
                    {
                        // 清空现有数据
                        // Clear existing data
                        await connection.ExecuteAsync("DELETE FROM WalletAccounts", transaction: transaction);
                    }
                    
                    // 插入备份的钱包数据
                    // Insert backed up wallet data
                    var insertSql = @"
                        INSERT OR REPLACE INTO WalletAccounts 
                        (Address, EncryptedPrivateKey, ChainType, Name, PasswordHash, IsCurrent)
                        VALUES (@Address, @EncryptedPrivateKey, @ChainType, @Name, @PasswordHash, @IsCurrent)";
                    
                    foreach (var wallet in backupWallets)
                    {
                        await connection.ExecuteAsync(insertSql, wallet, transaction: transaction);
                    }
                }
            });
        }

        /// <summary>
        /// 根据分组获取钱包
        /// Get wallets by group
        /// </summary>
        /// <param name="group">钱包分组 / Wallet group</param>
        public async Task<IEnumerable<WalletAccount>> GetByGroupAsync(string group)
        {
            const string sql = "SELECT * FROM WalletAccounts WHERE [Group] = @Group";
            return await QueryAsync<WalletAccount>(sql, new { Group = group });
        }

        /// <summary>
        /// 根据标签搜索钱包
        /// Search wallets by tag
        /// </summary>
        /// <param name="tag">钱包标签 / Wallet tag</param>
        public async Task<IEnumerable<WalletAccount>> SearchByTagAsync(string tag)
        {
            const string sql = "SELECT * FROM WalletAccounts WHERE Tags LIKE @TagPattern";
            return await QueryAsync<WalletAccount>(sql, new { TagPattern = $"%{tag}%" });
        }

        /// <summary>
        /// 获取所有钱包分组
        /// Get all wallet groups
        /// </summary>
        public async Task<IEnumerable<string>> GetAllGroupsAsync()
        {
            const string sql = "SELECT DISTINCT [Group] FROM WalletAccounts WHERE [Group] IS NOT NULL AND [Group] != ''";
            return await QueryAsync<string>(sql);
        }

        /// <summary>
        /// 获取所有钱包标签
        /// Get all wallet tags
        /// </summary>
        public async Task<IEnumerable<string>> GetAllTagsAsync()
        {
            // 获取所有标签字符串
            const string sql = "SELECT Tags FROM WalletAccounts WHERE Tags IS NOT NULL AND Tags != ''";
            var tagStrings = await QueryAsync<string>(sql);
            
            // 解析并去重
            var allTags = new HashSet<string>();
            foreach (var tagString in tagStrings)
            {
                var tags = tagString.Split(',').Select(t => t.Trim());
                foreach (var tag in tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        allTags.Add(tag);
                    }
                }
            }
            
            return allTags;
        }

        /// <summary>
        /// 更新钱包使用统计
        /// Update wallet usage statistics
        /// </summary>
        /// <param name="address">钱包地址 / Wallet address</param>
        public async Task UpdateUsageStatsAsync(string address)
        {
            const string sql = @"
                UPDATE WalletAccounts 
                SET UsageCount = UsageCount + 1, LastUsedAt = @LastUsedAt 
                WHERE Address = @Address";
            
            await ExecuteAsync(sql, new { Address = address, LastUsedAt = DateTime.UtcNow.ToString("o") });
        }

        /// <summary>
        /// 导出钱包到JSON文件
        /// Export wallets to JSON file
        /// </summary>
        /// <param name="exportPath">导出文件路径 / Export file path</param>
        /// <param name="addresses">要导出的钱包地址列表，为null则导出所有钱包 / List of wallet addresses to export, export all if null</param>
        public async Task ExportToJsonAsync(string exportPath, IEnumerable<string> addresses = null)
        {
            // 获取要导出的钱包
            IEnumerable<WalletAccount> wallets;
            if (addresses == null || !addresses.Any())
            {
                wallets = await GetAllAsync();
            }
            else
            {
                var walletList = new List<WalletAccount>();
                foreach (var address in addresses)
                {
                    var wallet = await GetByAddressAsync(address);
                    if (wallet != null)
                    {
                        walletList.Add(wallet);
                    }
                }
                wallets = walletList;
            }
            
            // 序列化为JSON
            var json = System.Text.Json.JsonSerializer.Serialize(wallets, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            // 写入文件
            await System.IO.File.WriteAllTextAsync(exportPath, json);
        }

        /// <summary>
        /// 从JSON文件导入钱包
        /// Import wallets from JSON file
        /// </summary>
        /// <param name="importPath">导入文件路径 / Import file path</param>
        /// <param name="overwrite">是否覆盖现有钱包 / Whether to overwrite existing wallets</param>
        /// <returns>导入的钱包数量 / Number of imported wallets</returns>
        public async Task<int> ImportFromJsonAsync(string importPath, bool overwrite = false)
        {
            // 读取JSON文件
            var json = await System.IO.File.ReadAllTextAsync(importPath);
            
            // 反序列化
            var wallets = System.Text.Json.JsonSerializer.Deserialize<List<WalletAccount>>(json);
            if (wallets == null || !wallets.Any())
            {
                return 0;
            }
            
            int importedCount = 0;
            
            // 导入钱包
            await ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                foreach (var wallet in wallets)
                {
                    // 检查钱包是否已存在
                    var exists = await ExistsAsync(wallet.Address);
                    
                    if (exists)
                    {
                        if (overwrite)
                        {
                            // 更新现有钱包
                            const string updateSql = @"
                                UPDATE WalletAccounts
                                SET EncryptedPrivateKey = @EncryptedPrivateKey,
                                    ChainType = @ChainType,
                                    Name = @Name,
                                    PasswordHash = @PasswordHash,
                                    IsCurrent = @IsCurrent,
                                    [Group] = @Group,
                                    Tags = @Tags,
                                    Metadata = @Metadata
                                WHERE Address = @Address";
                            
                            await connection.ExecuteAsync(updateSql, wallet, transaction: transaction);
                            importedCount++;
                        }
                        // 如果不覆盖，则跳过
                    }
                    else
                    {
                        // 添加新钱包
                        const string insertSql = @"
                            INSERT INTO WalletAccounts (
                                Address, EncryptedPrivateKey, ChainType, Name, PasswordHash, IsCurrent,
                                Group, Tags, Metadata, CreatedAt, LastUsedAt, UsageCount
                            )
                            VALUES (
                                @Address, @EncryptedPrivateKey, @ChainType, @Name, @PasswordHash, @IsCurrent,
                                @Group, @Tags, @Metadata, @CreatedAt, @LastUsedAt, @UsageCount
                            )";
                        
                        await connection.ExecuteAsync(insertSql, wallet, transaction: transaction);
                        importedCount++;
                    }
                }
            });
            
            return importedCount;
        }

        /// <summary>
        /// 更新钱包账户
        /// Update wallet account
        /// </summary>
        public async Task UpdateAsync(WalletAccount wallet)
        {
            const string sql = @"
                UPDATE WalletAccounts 
                SET EncryptedPrivateKey = @EncryptedPrivateKey,
                    ChainType = @ChainType,
                    Name = @Name,
                    PasswordHash = @PasswordHash,
                    IsCurrent = @IsCurrent
                WHERE Address = @Address";
            await ExecuteAsync(sql, wallet);
        }

        /// <summary>
        /// 删除钱包账户
        /// Delete wallet account
        /// </summary>
        public async Task DeleteAsync(string address)
        {
            const string sql = "DELETE FROM WalletAccounts WHERE Address = @Address";
            await ExecuteAsync(sql, new { Address = address });
        }

        /// <summary>
        /// 检查钱包是否存在
        /// Check if wallet exists
        /// </summary>
        public async Task<bool> ExistsAsync(string address)
        {
            const string sql = "SELECT COUNT(*) FROM WalletAccounts WHERE Address = @Address";
            var count = await QuerySingleOrDefaultAsync<int>(sql, new { Address = address });
            return count > 0;
        }

        /// <summary>
        /// 获取当前钱包
        /// Get current wallet
        /// </summary>
        public async Task<WalletAccount> GetCurrentWalletAsync()
        {
            const string sql = "SELECT * FROM WalletAccounts WHERE IsCurrent = 1";
            return await QuerySingleOrDefaultAsync<WalletAccount>(sql);
        }

        /// <summary>
        /// 设置当前钱包
        /// Set current wallet
        /// </summary>
        public async Task SetCurrentWalletAsync(string address)
        {
            await ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                // 先将所有钱包设置为非当前
                // First set all wallets to non-current
                var resetSql = "UPDATE WalletAccounts SET IsCurrent = 0";
                await connection.ExecuteAsync(resetSql, transaction: transaction);

                // 然后设置指定钱包为当前钱包
                // Then set the specified wallet as current
                var updateSql = "UPDATE WalletAccounts SET IsCurrent = 1 WHERE Address = @Address";
                await connection.ExecuteAsync(updateSql, new { Address = address }, transaction: transaction);
            });
        }
    }
}
