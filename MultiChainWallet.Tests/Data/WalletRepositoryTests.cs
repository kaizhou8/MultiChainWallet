using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiChainWallet.Core.Models;
using MultiChainWallet.Infrastructure.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MultiChainWallet.Tests.Data
{
    [TestClass]
    public class WalletRepositoryTests
    {
        private string _connectionString;
        private WalletRepository _walletRepository;

        [TestInitialize]
        public async Task Initialize()
        {
            // 使用内存数据库进行测试
            // Use in-memory database for testing
            _connectionString = "Data Source=:memory:";
            _walletRepository = new WalletRepository(_connectionString);

            // 创建测试数据库
            // Create test database
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // 创建钱包表
                // Create wallet table
                var createTableCommand = connection.CreateCommand();
                createTableCommand.CommandText = @"
                    CREATE TABLE WalletAccounts (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Address TEXT NOT NULL UNIQUE,
                        EncryptedPrivateKey TEXT NOT NULL,
                        ChainType TEXT NOT NULL,
                        Name TEXT,
                        PasswordHash TEXT,
                        IsCurrent INTEGER NOT NULL DEFAULT 0,
                        Group TEXT,
                        Tags TEXT,
                        Metadata TEXT,
                        CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                        LastUsedAt TEXT,
                        UsageCount INTEGER DEFAULT 0
                    )";
                await createTableCommand.ExecuteNonQueryAsync();
            }
        }

        private async Task InsertTestWallets(IEnumerable<WalletAccount> wallets)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                foreach (var wallet in wallets)
                {
                    const string sql = @"
                        INSERT INTO WalletAccounts (Address, EncryptedPrivateKey, ChainType, Name, PasswordHash, IsCurrent, Group, Tags, Metadata, CreatedAt, LastUsedAt, UsageCount)
                        VALUES (@Address, @EncryptedPrivateKey, @ChainType, @Name, @PasswordHash, @IsCurrent, @Group, @Tags, @Metadata, @CreatedAt, @LastUsedAt, @UsageCount)";
                    
                    await connection.ExecuteAsync(sql, wallet);
                }
            }
        }

        [TestMethod]
        public async Task GetAllAsync_ShouldReturnAllWallets()
        {
            // Arrange
            var testWallets = new List<WalletAccount>
            {
                new WalletAccount
                {
                    Address = "0x123",
                    EncryptedPrivateKey = "encrypted-key-1",
                    ChainType = "ETH",
                    Name = "Test Wallet 1",
                    PasswordHash = "hash1",
                    IsCurrent = true,
                    Group = "Group1",
                    Tags = "Tag1,Tag2",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 1
                },
                new WalletAccount
                {
                    Address = "0x456",
                    EncryptedPrivateKey = "encrypted-key-2",
                    ChainType = "BTC",
                    Name = "Test Wallet 2",
                    PasswordHash = "hash2",
                    IsCurrent = false,
                    Group = "Group2",
                    Tags = "Tag3,Tag4",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 2
                }
            };

            await InsertTestWallets(testWallets);

            // Act
            var result = await _walletRepository.GetAllAsync();

            // Assert
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Any(w => w.Address == "0x123"));
            Assert.IsTrue(result.Any(w => w.Address == "0x456"));
        }

        [TestMethod]
        public async Task GetByAddressAsync_WithExistingAddress_ShouldReturnWallet()
        {
            // Arrange
            var testWallet = new WalletAccount
            {
                Address = "0x789",
                EncryptedPrivateKey = "encrypted-key-3",
                ChainType = "ETH",
                Name = "Test Wallet 3",
                PasswordHash = "hash3",
                IsCurrent = false,
                Group = "Group3",
                Tags = "Tag5,Tag6",
                Metadata = "{\"key\":\"value\"}",
                CreatedAt = DateTime.UtcNow.ToString("o"),
                LastUsedAt = DateTime.UtcNow.ToString("o"),
                UsageCount = 3
            };

            await InsertTestWallets(new List<WalletAccount> { testWallet });

            // Act
            var result = await _walletRepository.GetByAddressAsync("0x789");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Test Wallet 3", result.Name);
        }

        [TestMethod]
        public async Task GetByAddressAsync_WithNonExistingAddress_ShouldReturnNull()
        {
            // Act
            var result = await _walletRepository.GetByAddressAsync("non-existing-address");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task AddAsync_ShouldAddWalletToDatabase()
        {
            // Arrange
            var newWallet = new WalletAccount
            {
                Address = "0xabc",
                EncryptedPrivateKey = "encrypted-key-4",
                ChainType = "ETH",
                Name = "Test Wallet 4",
                PasswordHash = "hash4",
                IsCurrent = false,
                Group = "Group4",
                Tags = "Tag7,Tag8",
                Metadata = "{\"key\":\"value\"}",
                CreatedAt = DateTime.UtcNow.ToString("o"),
                LastUsedAt = DateTime.UtcNow.ToString("o"),
                UsageCount = 4
            };

            // Act
            await _walletRepository.AddAsync(newWallet);

            // Assert
            var addedWallet = await _walletRepository.GetByAddressAsync("0xabc");
            Assert.IsNotNull(addedWallet);
            Assert.AreEqual("Test Wallet 4", addedWallet.Name);
            Assert.AreEqual("ETH", addedWallet.ChainType);
        }

        [TestMethod]
        public async Task UpdateAsync_ShouldUpdateWalletInDatabase()
        {
            // Arrange
            var testWallet = new WalletAccount
            {
                Address = "0xdef",
                EncryptedPrivateKey = "encrypted-key-5",
                ChainType = "ETH",
                Name = "Test Wallet 5",
                PasswordHash = "hash5",
                IsCurrent = false,
                Group = "Group5",
                Tags = "Tag9,Tag10",
                Metadata = "{\"key\":\"value\"}",
                CreatedAt = DateTime.UtcNow.ToString("o"),
                LastUsedAt = DateTime.UtcNow.ToString("o"),
                UsageCount = 5
            };

            await InsertTestWallets(new List<WalletAccount> { testWallet });

            // Act - Update the wallet
            testWallet.Name = "Updated Wallet 5";
            testWallet.IsCurrent = true;

            // Act
            await _walletRepository.UpdateAsync(testWallet);

            // Assert
            var updatedWallet = await _walletRepository.GetByAddressAsync("0xdef");
            Assert.IsNotNull(updatedWallet);
            Assert.AreEqual("Updated Wallet 5", updatedWallet.Name);
            Assert.IsTrue(updatedWallet.IsCurrent);
        }

        [TestMethod]
        public async Task DeleteAsync_ShouldRemoveWalletFromDatabase()
        {
            // Arrange
            var testWallet = new WalletAccount
            {
                Address = "0xghi",
                EncryptedPrivateKey = "encrypted-key-6",
                ChainType = "ETH",
                Name = "Test Wallet 6",
                PasswordHash = "hash6",
                IsCurrent = false,
                Group = "Group6",
                Tags = "Tag11,Tag12",
                Metadata = "{\"key\":\"value\"}",
                CreatedAt = DateTime.UtcNow.ToString("o"),
                LastUsedAt = DateTime.UtcNow.ToString("o"),
                UsageCount = 6
            };

            await InsertTestWallets(new List<WalletAccount> { testWallet });

            // Act
            await _walletRepository.DeleteAsync("0xghi");

            // Assert
            var deletedWallet = await _walletRepository.GetByAddressAsync("0xghi");
            Assert.IsNull(deletedWallet);
        }

        [TestMethod]
        public async Task SetCurrentWalletAsync_ShouldSetCurrentWallet()
        {
            // Arrange
            var testWallets = new List<WalletAccount>
            {
                new WalletAccount
                {
                    Address = "0xjkl",
                    EncryptedPrivateKey = "encrypted-key-7",
                    ChainType = "ETH",
                    Name = "Test Wallet 7",
                    PasswordHash = "hash7",
                    IsCurrent = true,
                    Group = "Group7",
                    Tags = "Tag13,Tag14",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 7
                },
                new WalletAccount
                {
                    Address = "0xmno",
                    EncryptedPrivateKey = "encrypted-key-8",
                    ChainType = "BTC",
                    Name = "Test Wallet 8",
                    PasswordHash = "hash8",
                    IsCurrent = false,
                    Group = "Group8",
                    Tags = "Tag15,Tag16",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 8
                }
            };

            await InsertTestWallets(testWallets);

            // Act
            await _walletRepository.SetCurrentWalletAsync("0xmno");

            // Assert
            var currentWallet = await _walletRepository.GetCurrentWalletAsync();
            var previousWallet = await _walletRepository.GetByAddressAsync("0xjkl");
            
            Assert.IsNotNull(currentWallet);
            Assert.AreEqual("0xmno", currentWallet.Address);
            Assert.IsFalse(previousWallet.IsCurrent);
        }

        [TestMethod]
        public async Task AddBatchAsync_ShouldAddMultipleWalletsToDatabase()
        {
            // Arrange
            var testWallets = new List<WalletAccount>
            {
                new WalletAccount
                {
                    Address = "0xbatch1",
                    EncryptedPrivateKey = "encrypted-key-batch-1",
                    ChainType = "ETH",
                    Name = "Batch Wallet 1",
                    PasswordHash = "hash-batch-1",
                    IsCurrent = false,
                    Group = "Group9",
                    Tags = "Tag17,Tag18",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 9
                },
                new WalletAccount
                {
                    Address = "0xbatch2",
                    EncryptedPrivateKey = "encrypted-key-batch-2",
                    ChainType = "BTC",
                    Name = "Batch Wallet 2",
                    PasswordHash = "hash-batch-2",
                    IsCurrent = false,
                    Group = "Group10",
                    Tags = "Tag19,Tag20",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 10
                },
                new WalletAccount
                {
                    Address = "0xbatch3",
                    EncryptedPrivateKey = "encrypted-key-batch-3",
                    ChainType = "ETH",
                    Name = "Batch Wallet 3",
                    PasswordHash = "hash-batch-3",
                    IsCurrent = false,
                    Group = "Group11",
                    Tags = "Tag21,Tag22",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 11
                }
            };

            // Act
            await _walletRepository.AddBatchAsync(testWallets);

            // Assert
            var allWallets = await _walletRepository.GetAllAsync();
            Assert.AreEqual(3, allWallets.Count());
            Assert.IsTrue(allWallets.Any(w => w.Address == "0xbatch1"));
            Assert.IsTrue(allWallets.Any(w => w.Address == "0xbatch2"));
            Assert.IsTrue(allWallets.Any(w => w.Address == "0xbatch3"));
        }

        [TestMethod]
        public async Task GetByChainTypeAsync_ShouldReturnWalletsOfSpecifiedChainType()
        {
            // Arrange
            var testWallets = new List<WalletAccount>
            {
                new WalletAccount
                {
                    Address = "0xchain1",
                    EncryptedPrivateKey = "encrypted-key-chain-1",
                    ChainType = "ETH",
                    Name = "ETH Wallet 1",
                    PasswordHash = "hash-chain-1",
                    IsCurrent = false,
                    Group = "Group12",
                    Tags = "Tag23,Tag24",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 12
                },
                new WalletAccount
                {
                    Address = "0xchain2",
                    EncryptedPrivateKey = "encrypted-key-chain-2",
                    ChainType = "BTC",
                    Name = "BTC Wallet 1",
                    PasswordHash = "hash-chain-2",
                    IsCurrent = false,
                    Group = "Group13",
                    Tags = "Tag25,Tag26",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 13
                },
                new WalletAccount
                {
                    Address = "0xchain3",
                    EncryptedPrivateKey = "encrypted-key-chain-3",
                    ChainType = "ETH",
                    Name = "ETH Wallet 2",
                    PasswordHash = "hash-chain-3",
                    IsCurrent = false,
                    Group = "Group14",
                    Tags = "Tag27,Tag28",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 14
                }
            };

            await InsertTestWallets(testWallets);

            // Act
            var ethWallets = await _walletRepository.GetByChainTypeAsync("ETH");

            // Assert
            Assert.AreEqual(2, ethWallets.Count());
            Assert.IsTrue(ethWallets.All(w => w.ChainType == "ETH"));
            Assert.IsTrue(ethWallets.Any(w => w.Address == "0xchain1"));
            Assert.IsTrue(ethWallets.Any(w => w.Address == "0xchain3"));
        }

        [TestMethod]
        public async Task GetTotalCountAsync_ShouldReturnCorrectCount()
        {
            // Arrange
            var testWallets = new List<WalletAccount>
            {
                new WalletAccount
                {
                    Address = "0xcount1",
                    EncryptedPrivateKey = "encrypted-key-count-1",
                    ChainType = "ETH",
                    Name = "Count Wallet 1",
                    PasswordHash = "hash-count-1",
                    IsCurrent = false,
                    Group = "Group15",
                    Tags = "Tag29,Tag30",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 15
                },
                new WalletAccount
                {
                    Address = "0xcount2",
                    EncryptedPrivateKey = "encrypted-key-count-2",
                    ChainType = "BTC",
                    Name = "Count Wallet 2",
                    PasswordHash = "hash-count-2",
                    IsCurrent = false,
                    Group = "Group16",
                    Tags = "Tag31,Tag32",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 16
                },
                new WalletAccount
                {
                    Address = "0xcount3",
                    EncryptedPrivateKey = "encrypted-key-count-3",
                    ChainType = "ETH",
                    Name = "Count Wallet 3",
                    PasswordHash = "hash-count-3",
                    IsCurrent = false,
                    Group = "Group17",
                    Tags = "Tag33,Tag34",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 17
                },
                new WalletAccount
                {
                    Address = "0xcount4",
                    EncryptedPrivateKey = "encrypted-key-count-4",
                    ChainType = "SOL",
                    Name = "Count Wallet 4",
                    PasswordHash = "hash-count-4",
                    IsCurrent = false,
                    Group = "Group18",
                    Tags = "Tag35,Tag36",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 18
                }
            };

            await InsertTestWallets(testWallets);

            // Act
            var count = await _walletRepository.GetTotalCountAsync();

            // Assert
            Assert.AreEqual(4, count);
        }

        [TestMethod]
        public async Task BackupAndRestoreAsync_ShouldCorrectlyBackupAndRestoreWallets()
        {
            // Arrange
            var testWallets = new List<WalletAccount>
            {
                new WalletAccount
                {
                    Address = "0xbackup1",
                    EncryptedPrivateKey = "encrypted-key-backup-1",
                    ChainType = "ETH",
                    Name = "Backup Wallet 1",
                    PasswordHash = "hash-backup-1",
                    IsCurrent = true,
                    Group = "Group19",
                    Tags = "Tag37,Tag38",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 19
                },
                new WalletAccount
                {
                    Address = "0xbackup2",
                    EncryptedPrivateKey = "encrypted-key-backup-2",
                    ChainType = "BTC",
                    Name = "Backup Wallet 2",
                    PasswordHash = "hash-backup-2",
                    IsCurrent = false,
                    Group = "Group20",
                    Tags = "Tag39,Tag40",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 20
                }
            };

            await InsertTestWallets(testWallets);

            // 创建临时备份文件
            // Create temporary backup file
            var backupPath = Path.Combine(Path.GetTempPath(), $"wallet_backup_{Guid.NewGuid()}.db");

            try
            {
                // Act - Backup
                await _walletRepository.BackupAsync(backupPath);

                // 清空数据库
                // Clear database
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    await connection.ExecuteAsync("DELETE FROM WalletAccounts");
                }

                // 验证数据库已清空
                // Verify database is empty
                var emptyWallets = await _walletRepository.GetAllAsync();
                Assert.AreEqual(0, emptyWallets.Count());

                // Act - Restore
                await _walletRepository.RestoreAsync(backupPath);

                // Assert
                var restoredWallets = await _walletRepository.GetAllAsync();
                Assert.AreEqual(2, restoredWallets.Count());
                
                var wallet1 = restoredWallets.FirstOrDefault(w => w.Address == "0xbackup1");
                var wallet2 = restoredWallets.FirstOrDefault(w => w.Address == "0xbackup2");
                
                Assert.IsNotNull(wallet1);
                Assert.IsNotNull(wallet2);
                Assert.AreEqual("Backup Wallet 1", wallet1.Name);
                Assert.AreEqual("Backup Wallet 2", wallet2.Name);
                Assert.AreEqual("ETH", wallet1.ChainType);
                Assert.AreEqual("BTC", wallet2.ChainType);
                Assert.IsTrue(wallet1.IsCurrent);
                Assert.IsFalse(wallet2.IsCurrent);
            }
            finally
            {
                // 清理临时文件
                // Clean up temporary file
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
        }

        [TestMethod]
        public async Task RestoreAsync_WithOverwriteFalse_ShouldMergeWallets()
        {
            // Arrange - 初始钱包
            // Arrange - Initial wallets
            var initialWallets = new List<WalletAccount>
            {
                new WalletAccount
                {
                    Address = "0xmerge1",
                    EncryptedPrivateKey = "encrypted-key-merge-1",
                    ChainType = "ETH",
                    Name = "Merge Wallet 1",
                    PasswordHash = "hash-merge-1",
                    IsCurrent = true,
                    Group = "Group21",
                    Tags = "Tag41,Tag42",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 21
                }
            };

            await InsertTestWallets(initialWallets);

            // 创建临时备份文件
            // Create temporary backup file
            var backupPath = Path.Combine(Path.GetTempPath(), $"wallet_backup_merge_{Guid.NewGuid()}.db");

            try
            {
                // 创建备份数据库并添加钱包
                // Create backup database and add wallets
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
                            Group TEXT,
                            Tags TEXT,
                            Metadata TEXT,
                            CreatedAt TEXT,
                            LastUsedAt TEXT,
                            UsageCount INTEGER
                        )";
                    
                    await backupConnection.ExecuteAsync(createTableSql);
                    
                    // 添加备份钱包
                    // Add backup wallets
                    var backupWallets = new List<WalletAccount>
                    {
                        new WalletAccount
                        {
                            Address = "0xmerge2",
                            EncryptedPrivateKey = "encrypted-key-merge-2",
                            ChainType = "BTC",
                            Name = "Merge Wallet 2",
                            PasswordHash = "hash-merge-2",
                            IsCurrent = false,
                            Group = "Group22",
                            Tags = "Tag43,Tag44",
                            Metadata = "{\"key\":\"value\"}",
                            CreatedAt = DateTime.UtcNow.ToString("o"),
                            LastUsedAt = DateTime.UtcNow.ToString("o"),
                            UsageCount = 22
                        },
                        new WalletAccount
                        {
                            Address = "0xmerge3",
                            EncryptedPrivateKey = "encrypted-key-merge-3",
                            ChainType = "SOL",
                            Name = "Merge Wallet 3",
                            PasswordHash = "hash-merge-3",
                            IsCurrent = false,
                            Group = "Group23",
                            Tags = "Tag45,Tag46",
                            Metadata = "{\"key\":\"value\"}",
                            CreatedAt = DateTime.UtcNow.ToString("o"),
                            LastUsedAt = DateTime.UtcNow.ToString("o"),
                            UsageCount = 23
                        }
                    };
                    
                    foreach (var wallet in backupWallets)
                    {
                        var insertSql = @"
                            INSERT INTO WalletAccounts 
                            (Address, EncryptedPrivateKey, ChainType, Name, PasswordHash, IsCurrent, Group, Tags, Metadata, CreatedAt, LastUsedAt, UsageCount)
                            VALUES (@Address, @EncryptedPrivateKey, @ChainType, @Name, @PasswordHash, @IsCurrent, @Group, @Tags, @Metadata, @CreatedAt, @LastUsedAt, @UsageCount)";
                        
                        await backupConnection.ExecuteAsync(insertSql, new
                        {
                            wallet.Address,
                            wallet.EncryptedPrivateKey,
                            wallet.ChainType,
                            wallet.Name,
                            wallet.PasswordHash,
                            wallet.IsCurrent,
                            wallet.Group,
                            wallet.Tags,
                            wallet.Metadata,
                            wallet.CreatedAt,
                            wallet.LastUsedAt,
                            wallet.UsageCount
                        });
                    }
                }

                // Act - 恢复钱包，不覆盖现有数据
                // Act - Restore wallets without overwriting existing data
                await _walletRepository.RestoreAsync(backupPath, overwrite: false);

                // Assert
                var mergedWallets = await _walletRepository.GetAllAsync();
                Assert.AreEqual(3, mergedWallets.Count());
                
                // 验证原有钱包仍然存在
                // Verify original wallet still exists
                var originalWallet = mergedWallets.FirstOrDefault(w => w.Address == "0xmerge1");
                Assert.IsNotNull(originalWallet);
                Assert.AreEqual("Merge Wallet 1", originalWallet.Name);
                Assert.IsTrue(originalWallet.IsCurrent);
                
                // 验证备份的钱包已添加
                // Verify backup wallets were added
                Assert.IsTrue(mergedWallets.Any(w => w.Address == "0xmerge2"));
                Assert.IsTrue(mergedWallets.Any(w => w.Address == "0xmerge3"));
            }
            finally
            {
                // 清理临时文件
                // Clean up temporary file
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
        }

        [TestMethod]
        public async Task RestoreAsync_WithOverwriteTrue_ShouldReplaceAllWallets()
        {
            // Arrange - 初始钱包
            // Arrange - Initial wallets
            var initialWallets = new List<WalletAccount>
            {
                new WalletAccount
                {
                    Address = "0xoriginal1",
                    EncryptedPrivateKey = "encrypted-key-original-1",
                    ChainType = "ETH",
                    Name = "Original Wallet 1",
                    PasswordHash = "hash-original-1",
                    IsCurrent = true,
                    Group = "Group24",
                    Tags = "Tag47,Tag48",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 24
                },
                new WalletAccount
                {
                    Address = "0xoriginal2",
                    EncryptedPrivateKey = "encrypted-key-original-2",
                    ChainType = "BTC",
                    Name = "Original Wallet 2",
                    PasswordHash = "hash-original-2",
                    IsCurrent = false,
                    Group = "Group25",
                    Tags = "Tag49,Tag50",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 25
                }
            };

            await InsertTestWallets(initialWallets);

            // 创建临时备份文件
            // Create temporary backup file
            var backupPath = Path.Combine(Path.GetTempPath(), $"wallet_backup_overwrite_{Guid.NewGuid()}.db");

            try
            {
                // 创建备份数据库并添加钱包
                // Create backup database and add wallets
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
                            Group TEXT,
                            Tags TEXT,
                            Metadata TEXT,
                            CreatedAt TEXT,
                            LastUsedAt TEXT,
                            UsageCount INTEGER
                        )";
                    
                    await backupConnection.ExecuteAsync(createTableSql);
                    
                    // 添加备份钱包
                    // Add backup wallets
                    var backupWallets = new List<WalletAccount>
                    {
                        new WalletAccount
                        {
                            Address = "0xbackup1",
                            EncryptedPrivateKey = "encrypted-key-backup-1",
                            ChainType = "ETH",
                            Name = "Backup Wallet 1",
                            PasswordHash = "hash-backup-1",
                            IsCurrent = true,
                            Group = "Group26",
                            Tags = "Tag51,Tag52",
                            Metadata = "{\"key\":\"value\"}",
                            CreatedAt = DateTime.UtcNow.ToString("o"),
                            LastUsedAt = DateTime.UtcNow.ToString("o"),
                            UsageCount = 26
                        }
                    };
                    
                    foreach (var wallet in backupWallets)
                    {
                        var insertSql = @"
                            INSERT INTO WalletAccounts 
                            (Address, EncryptedPrivateKey, ChainType, Name, PasswordHash, IsCurrent, Group, Tags, Metadata, CreatedAt, LastUsedAt, UsageCount)
                            VALUES (@Address, @EncryptedPrivateKey, @ChainType, @Name, @PasswordHash, @IsCurrent, @Group, @Tags, @Metadata, @CreatedAt, @LastUsedAt, @UsageCount)";
                        
                        await backupConnection.ExecuteAsync(insertSql, new
                        {
                            wallet.Address,
                            wallet.EncryptedPrivateKey,
                            wallet.ChainType,
                            wallet.Name,
                            wallet.PasswordHash,
                            wallet.IsCurrent,
                            wallet.Group,
                            wallet.Tags,
                            wallet.Metadata,
                            wallet.CreatedAt,
                            wallet.LastUsedAt,
                            wallet.UsageCount
                        });
                    }
                }

                // Act - 恢复钱包，覆盖现有数据
                // Act - Restore wallets with overwriting existing data
                await _walletRepository.RestoreAsync(backupPath, overwrite: true);

                // Assert
                var restoredWallets = await _walletRepository.GetAllAsync();
                Assert.AreEqual(1, restoredWallets.Count());
                
                // 验证原有钱包已被删除
                // Verify original wallets were deleted
                Assert.IsFalse(restoredWallets.Any(w => w.Address == "0xoriginal1"));
                Assert.IsFalse(restoredWallets.Any(w => w.Address == "0xoriginal2"));
                
                // 验证备份的钱包已添加
                // Verify backup wallet was added
                var backupWallet = restoredWallets.FirstOrDefault(w => w.Address == "0xbackup1");
                Assert.IsNotNull(backupWallet);
                Assert.AreEqual("Backup Wallet 1", backupWallet.Name);
                Assert.AreEqual("ETH", backupWallet.ChainType);
                Assert.IsTrue(backupWallet.IsCurrent);
            }
            finally
            {
                // 清理临时文件
                // Clean up temporary file
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
        }

        [TestMethod]
        public async Task GetByGroupAsync_ShouldReturnWalletsInGroup()
        {
            // Arrange
            var testWallets = new List<WalletAccount>
            {
                new WalletAccount
                {
                    Address = "0xGroup1Wallet1",
                    EncryptedPrivateKey = "encrypted-key-group1-1",
                    ChainType = "ETH",
                    Name = "Group 1 Wallet 1",
                    PasswordHash = "hash-group1-1",
                    IsCurrent = false,
                    Group = "TestGroup1",
                    Tags = "Tag1,Tag2",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 1
                },
                new WalletAccount
                {
                    Address = "0xGroup1Wallet2",
                    EncryptedPrivateKey = "encrypted-key-group1-2",
                    ChainType = "BTC",
                    Name = "Group 1 Wallet 2",
                    PasswordHash = "hash-group1-2",
                    IsCurrent = false,
                    Group = "TestGroup1",
                    Tags = "Tag3,Tag4",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 2
                },
                new WalletAccount
                {
                    Address = "0xGroup2Wallet1",
                    EncryptedPrivateKey = "encrypted-key-group2-1",
                    ChainType = "ETH",
                    Name = "Group 2 Wallet 1",
                    PasswordHash = "hash-group2-1",
                    IsCurrent = false,
                    Group = "TestGroup2",
                    Tags = "Tag5,Tag6",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 3
                }
            };

            await InsertTestWallets(testWallets);

            // Act
            var group1Wallets = await _walletRepository.GetByGroupAsync("TestGroup1");

            // Assert
            Assert.AreEqual(2, group1Wallets.Count());
            Assert.IsTrue(group1Wallets.All(w => w.Group == "TestGroup1"));
            Assert.AreEqual("0xGroup1Wallet1", group1Wallets.First().Address);
            Assert.AreEqual("0xGroup1Wallet2", group1Wallets.Last().Address);
        }

        [TestMethod]
        public async Task SearchByTagAsync_ShouldReturnWalletsWithTag()
        {
            // Arrange
            var testWallets = new List<WalletAccount>
            {
                new WalletAccount
                {
                    Address = "0xTagWallet1",
                    EncryptedPrivateKey = "encrypted-key-tag1",
                    ChainType = "ETH",
                    Name = "Tag Wallet 1",
                    PasswordHash = "hash-tag1",
                    IsCurrent = false,
                    Group = "TagGroup1",
                    Tags = "SearchTag,Tag2",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 1
                },
                new WalletAccount
                {
                    Address = "0xTagWallet2",
                    EncryptedPrivateKey = "encrypted-key-tag2",
                    ChainType = "BTC",
                    Name = "Tag Wallet 2",
                    PasswordHash = "hash-tag2",
                    IsCurrent = false,
                    Group = "TagGroup2",
                    Tags = "Tag3,SearchTag",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 2
                },
                new WalletAccount
                {
                    Address = "0xTagWallet3",
                    EncryptedPrivateKey = "encrypted-key-tag3",
                    ChainType = "ETH",
                    Name = "Tag Wallet 3",
                    PasswordHash = "hash-tag3",
                    IsCurrent = false,
                    Group = "TagGroup3",
                    Tags = "Tag5,Tag6",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 3
                }
            };

            await InsertTestWallets(testWallets);

            // Act
            var taggedWallets = await _walletRepository.SearchByTagAsync("SearchTag");

            // Assert
            Assert.AreEqual(2, taggedWallets.Count());
            Assert.IsTrue(taggedWallets.All(w => w.Tags.Contains("SearchTag")));
            Assert.AreEqual("0xTagWallet1", taggedWallets.First().Address);
            Assert.AreEqual("0xTagWallet2", taggedWallets.Last().Address);
        }

        [TestMethod]
        public async Task GetAllGroupsAsync_ShouldReturnAllGroups()
        {
            // Arrange
            var testWallets = new List<WalletAccount>
            {
                new WalletAccount
                {
                    Address = "0xGroupTest1",
                    EncryptedPrivateKey = "encrypted-key-group-test1",
                    ChainType = "ETH",
                    Name = "Group Test Wallet 1",
                    PasswordHash = "hash-group-test1",
                    IsCurrent = false,
                    Group = "GroupTest1",
                    Tags = "Tag1,Tag2",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 1
                },
                new WalletAccount
                {
                    Address = "0xGroupTest2",
                    EncryptedPrivateKey = "encrypted-key-group-test2",
                    ChainType = "BTC",
                    Name = "Group Test Wallet 2",
                    PasswordHash = "hash-group-test2",
                    IsCurrent = false,
                    Group = "GroupTest2",
                    Tags = "Tag3,Tag4",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 2
                },
                new WalletAccount
                {
                    Address = "0xGroupTest3",
                    EncryptedPrivateKey = "encrypted-key-group-test3",
                    ChainType = "ETH",
                    Name = "Group Test Wallet 3",
                    PasswordHash = "hash-group-test3",
                    IsCurrent = false,
                    Group = "GroupTest1", // 重复的组
                    Tags = "Tag5,Tag6",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 3
                }
            };

            await InsertTestWallets(testWallets);

            // Act
            var groups = await _walletRepository.GetAllGroupsAsync();

            // Assert
            Assert.AreEqual(2, groups.Count());
            Assert.IsTrue(groups.Contains("GroupTest1"));
            Assert.IsTrue(groups.Contains("GroupTest2"));
        }

        [TestMethod]
        public async Task GetAllTagsAsync_ShouldReturnAllUniqueTags()
        {
            // Arrange
            var testWallets = new List<WalletAccount>
            {
                new WalletAccount
                {
                    Address = "0xTagTest1",
                    EncryptedPrivateKey = "encrypted-key-tag-test1",
                    ChainType = "ETH",
                    Name = "Tag Test Wallet 1",
                    PasswordHash = "hash-tag-test1",
                    IsCurrent = false,
                    Group = "TagTestGroup1",
                    Tags = "UniqueTag1,SharedTag",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 1
                },
                new WalletAccount
                {
                    Address = "0xTagTest2",
                    EncryptedPrivateKey = "encrypted-key-tag-test2",
                    ChainType = "BTC",
                    Name = "Tag Test Wallet 2",
                    PasswordHash = "hash-tag-test2",
                    IsCurrent = false,
                    Group = "TagTestGroup2",
                    Tags = "UniqueTag2,SharedTag",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 2
                }
            };

            await InsertTestWallets(testWallets);

            // Act
            var tags = await _walletRepository.GetAllTagsAsync();

            // Assert
            Assert.AreEqual(3, tags.Count());
            Assert.IsTrue(tags.Contains("UniqueTag1"));
            Assert.IsTrue(tags.Contains("UniqueTag2"));
            Assert.IsTrue(tags.Contains("SharedTag"));
        }

        [TestMethod]
        public async Task UpdateUsageStatsAsync_ShouldIncrementUsageCount()
        {
            // Arrange
            var testWallet = new WalletAccount
            {
                Address = "0xUsageStats",
                EncryptedPrivateKey = "encrypted-key-usage",
                ChainType = "ETH",
                Name = "Usage Stats Wallet",
                PasswordHash = "hash-usage",
                IsCurrent = false,
                Group = "UsageGroup",
                Tags = "UsageTag1,UsageTag2",
                Metadata = "{\"key\":\"value\"}",
                CreatedAt = DateTime.UtcNow.ToString("o"),
                LastUsedAt = null,
                UsageCount = 0
            };

            await InsertTestWallets(new List<WalletAccount> { testWallet });

            // Act
            await _walletRepository.UpdateUsageStatsAsync("0xUsageStats");
            var updatedWallet = await _walletRepository.GetByAddressAsync("0xUsageStats");

            // Assert
            Assert.AreEqual(1, updatedWallet.UsageCount);
            Assert.IsNotNull(updatedWallet.LastUsedAt);
        }

        [TestMethod]
        public async Task ExportToJsonAndImportFromJson_ShouldWorkCorrectly()
        {
            // Arrange
            var testWallets = new List<WalletAccount>
            {
                new WalletAccount
                {
                    Address = "0xExportWallet1",
                    EncryptedPrivateKey = "encrypted-key-export1",
                    ChainType = "ETH",
                    Name = "Export Wallet 1",
                    PasswordHash = "hash-export1",
                    IsCurrent = false,
                    Group = "ExportGroup",
                    Tags = "ExportTag1,ExportTag2",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 1
                },
                new WalletAccount
                {
                    Address = "0xExportWallet2",
                    EncryptedPrivateKey = "encrypted-key-export2",
                    ChainType = "BTC",
                    Name = "Export Wallet 2",
                    PasswordHash = "hash-export2",
                    IsCurrent = false,
                    Group = "ExportGroup",
                    Tags = "ExportTag3,ExportTag4",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 2
                }
            };

            await InsertTestWallets(testWallets);

            var exportPath = Path.Combine(Path.GetTempPath(), "wallet_export_test.json");
            var importPath = Path.Combine(Path.GetTempPath(), "wallet_import_test.json");

            // 确保文件不存在
            if (File.Exists(exportPath))
                File.Delete(exportPath);
            if (File.Exists(importPath))
                File.Delete(importPath);

            try
            {
                // Act - 导出
                await _walletRepository.ExportToJsonAsync(exportPath);

                // 验证导出文件存在
                Assert.IsTrue(File.Exists(exportPath));

                // 修改导出文件内容，添加一个新钱包
                var exportedJson = await File.ReadAllTextAsync(exportPath);
                var exportedWallets = System.Text.Json.JsonSerializer.Deserialize<List<WalletAccount>>(exportedJson);
                
                // 添加一个新钱包
                exportedWallets.Add(new WalletAccount
                {
                    Address = "0xImportWallet",
                    EncryptedPrivateKey = "encrypted-key-import",
                    ChainType = "SOL",
                    Name = "Import Wallet",
                    PasswordHash = "hash-import",
                    IsCurrent = false,
                    Group = "ImportGroup",
                    Tags = "ImportTag1,ImportTag2",
                    Metadata = "{\"key\":\"value\"}",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    LastUsedAt = DateTime.UtcNow.ToString("o"),
                    UsageCount = 3
                });

                var modifiedJson = System.Text.Json.JsonSerializer.Serialize(exportedWallets, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(importPath, modifiedJson);

                // Act - 导入
                var importedCount = await _walletRepository.ImportFromJsonAsync(importPath);

                // Assert
                Assert.AreEqual(1, importedCount); // 只有一个新钱包被导入
                
                // 验证新钱包是否导入成功
                var importedWallet = await _walletRepository.GetByAddressAsync("0xImportWallet");
                Assert.IsNotNull(importedWallet);
                Assert.AreEqual("SOL", importedWallet.ChainType);
                Assert.AreEqual("ImportGroup", importedWallet.Group);
            }
            finally
            {
                // 清理
                if (File.Exists(exportPath))
                    File.Delete(exportPath);
                if (File.Exists(importPath))
                    File.Delete(importPath);
            }
        }

        [TestMethod]
        public async Task ImportFromJson_WithOverwrite_ShouldUpdateExistingWallets()
        {
            // Arrange
            var testWallet = new WalletAccount
            {
                Address = "0xOverwriteWallet",
                EncryptedPrivateKey = "encrypted-key-overwrite",
                ChainType = "ETH",
                Name = "Original Wallet",
                PasswordHash = "hash-overwrite",
                IsCurrent = false,
                Group = "OriginalGroup",
                Tags = "OriginalTag1,OriginalTag2",
                Metadata = "{\"key\":\"original\"}",
                CreatedAt = DateTime.UtcNow.ToString("o"),
                LastUsedAt = DateTime.UtcNow.ToString("o"),
                UsageCount = 1
            };

            await InsertTestWallets(new List<WalletAccount> { testWallet });

            var importPath = Path.Combine(Path.GetTempPath(), "wallet_overwrite_test.json");

            // 确保文件不存在
            if (File.Exists(importPath))
                File.Delete(importPath);

            try
            {
                // 创建导入文件，包含更新的钱包信息
                var importWallets = new List<WalletAccount>
                {
                    new WalletAccount
                    {
                        Address = "0xOverwriteWallet", // 相同地址
                        EncryptedPrivateKey = "encrypted-key-new",
                        ChainType = "BTC", // 更改链类型
                        Name = "Updated Wallet", // 更改名称
                        PasswordHash = "hash-new",
                        IsCurrent = true, // 更改当前状态
                        Group = "UpdatedGroup", // 更改分组
                        Tags = "UpdatedTag1,UpdatedTag2", // 更改标签
                        Metadata = "{\"key\":\"updated\"}", // 更改元数据
                        CreatedAt = DateTime.UtcNow.ToString("o"),
                        LastUsedAt = DateTime.UtcNow.ToString("o"),
                        UsageCount = 5 // 更改使用次数
                    }
                };

                var importJson = System.Text.Json.JsonSerializer.Serialize(importWallets, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(importPath, importJson);

                // Act - 导入并覆盖
                var importedCount = await _walletRepository.ImportFromJsonAsync(importPath, true);

                // Assert
                Assert.AreEqual(1, importedCount);
                
                // 验证钱包是否被更新
                var updatedWallet = await _walletRepository.GetByAddressAsync("0xOverwriteWallet");
                Assert.IsNotNull(updatedWallet);
                Assert.AreEqual("BTC", updatedWallet.ChainType);
                Assert.AreEqual("Updated Wallet", updatedWallet.Name);
                Assert.AreEqual("UpdatedGroup", updatedWallet.Group);
                Assert.AreEqual("UpdatedTag1,UpdatedTag2", updatedWallet.Tags);
                Assert.AreEqual("{\"key\":\"updated\"}", updatedWallet.Metadata);
                Assert.IsTrue(updatedWallet.IsCurrent);
            }
            finally
            {
                // 清理
                if (File.Exists(importPath))
                    File.Delete(importPath);
            }
        }
    }
}
