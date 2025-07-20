using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiChainWallet.Core.Models;
using MultiChainWallet.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MultiChainWallet.Tests.Data
{
    [TestClass]
    public class TokenBalanceRepositoryTests
    {
        private string _connectionString;
        private TokenBalanceRepository _tokenBalanceRepository;

        [TestInitialize]
        public async Task Initialize()
        {
            // 使用内存数据库进行测试
            // Use in-memory database for testing
            _connectionString = "Data Source=:memory:";
            _tokenBalanceRepository = new TokenBalanceRepository(_connectionString);

            // 创建测试数据库
            // Create test database
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // 创建代币余额表
                // Create token balance table
                var createTableCommand = connection.CreateCommand();
                createTableCommand.CommandText = @"
                    CREATE TABLE TokenBalances (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        WalletAddress TEXT NOT NULL,
                        TokenAddress TEXT NOT NULL,
                        Balance TEXT NOT NULL,
                        ChainType TEXT NOT NULL,
                        TokenName TEXT,
                        TokenSymbol TEXT,
                        Decimals INTEGER,
                        LastUpdated TEXT,
                        UNIQUE(WalletAddress, TokenAddress, ChainType)
                    )";
                await createTableCommand.ExecuteNonQueryAsync();
            }
        }

        private async Task InsertTestTokenBalances(List<TokenBalance> tokenBalances)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                foreach (var balance in tokenBalances)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT INTO TokenBalances (WalletAddress, TokenAddress, Balance, ChainType, TokenName, TokenSymbol, Decimals, LastUpdated)
                        VALUES (@WalletAddress, @TokenAddress, @Balance, @ChainType, @TokenName, @TokenSymbol, @Decimals, @LastUpdated)";
                    command.Parameters.AddWithValue("@WalletAddress", balance.WalletAddress);
                    command.Parameters.AddWithValue("@TokenAddress", balance.TokenAddress);
                    command.Parameters.AddWithValue("@Balance", balance.Balance);
                    command.Parameters.AddWithValue("@ChainType", balance.ChainType);
                    command.Parameters.AddWithValue("@TokenName", balance.TokenName);
                    command.Parameters.AddWithValue("@TokenSymbol", balance.TokenSymbol);
                    command.Parameters.AddWithValue("@Decimals", balance.Decimals);
                    command.Parameters.AddWithValue("@LastUpdated", balance.LastUpdated.ToString("o"));
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        [TestMethod]
        public async Task GetAllForWalletAsync_ShouldReturnAllTokenBalancesForWallet()
        {
            // Arrange
            var testBalances = new List<TokenBalance>
            {
                new TokenBalance
                {
                    WalletAddress = "0x123",
                    TokenAddress = "0xtoken1",
                    Balance = "100",
                    ChainType = "ETH",
                    TokenName = "Token 1",
                    TokenSymbol = "TK1",
                    Decimals = 18,
                    LastUpdated = System.DateTime.UtcNow
                },
                new TokenBalance
                {
                    WalletAddress = "0x123",
                    TokenAddress = "0xtoken2",
                    Balance = "200",
                    ChainType = "ETH",
                    TokenName = "Token 2",
                    TokenSymbol = "TK2",
                    Decimals = 18,
                    LastUpdated = System.DateTime.UtcNow
                },
                new TokenBalance
                {
                    WalletAddress = "0x456",
                    TokenAddress = "0xtoken1",
                    Balance = "300",
                    ChainType = "ETH",
                    TokenName = "Token 1",
                    TokenSymbol = "TK1",
                    Decimals = 18,
                    LastUpdated = System.DateTime.UtcNow
                }
            };

            await InsertTestTokenBalances(testBalances);

            // Act
            var result = await _tokenBalanceRepository.GetAllForWalletAsync("0x123");

            // Assert
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.All(b => b.WalletAddress == "0x123"));
            Assert.IsTrue(result.Any(b => b.TokenAddress == "0xtoken1" && b.Balance == "100"));
            Assert.IsTrue(result.Any(b => b.TokenAddress == "0xtoken2" && b.Balance == "200"));
        }

        [TestMethod]
        public async Task GetByWalletAndTokenAsync_WithExistingBalance_ShouldReturnTokenBalance()
        {
            // Arrange
            var testBalance = new TokenBalance
            {
                WalletAddress = "0x789",
                TokenAddress = "0xtoken3",
                Balance = "500",
                ChainType = "ETH",
                TokenName = "Token 3",
                TokenSymbol = "TK3",
                Decimals = 18,
                LastUpdated = System.DateTime.UtcNow
            };

            await InsertTestTokenBalances(new List<TokenBalance> { testBalance });

            // Act
            var result = await _tokenBalanceRepository.GetByWalletAndTokenAsync("0x789", "0xtoken3", "ETH");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("0x789", result.WalletAddress);
            Assert.AreEqual("0xtoken3", result.TokenAddress);
            Assert.AreEqual("500", result.Balance);
        }

        [TestMethod]
        public async Task GetByWalletAndTokenAsync_WithNonExistingBalance_ShouldReturnNull()
        {
            // Act
            var result = await _tokenBalanceRepository.GetByWalletAndTokenAsync("non-existing-wallet", "non-existing-token", "ETH");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task AddOrUpdateAsync_WithNewTokenBalance_ShouldAddToDatabase()
        {
            // Arrange
            var newBalance = new TokenBalance
            {
                WalletAddress = "0xabc",
                TokenAddress = "0xtoken4",
                Balance = "1000",
                ChainType = "ETH",
                TokenName = "Token 4",
                TokenSymbol = "TK4",
                Decimals = 18,
                LastUpdated = System.DateTime.UtcNow
            };

            // Act
            await _tokenBalanceRepository.AddOrUpdateAsync(newBalance);

            // Assert
            var addedBalance = await _tokenBalanceRepository.GetByWalletAndTokenAsync("0xabc", "0xtoken4", "ETH");
            Assert.IsNotNull(addedBalance);
            Assert.AreEqual("1000", addedBalance.Balance);
            Assert.AreEqual("Token 4", addedBalance.TokenName);
        }

        [TestMethod]
        public async Task AddOrUpdateAsync_WithExistingTokenBalance_ShouldUpdateInDatabase()
        {
            // Arrange
            var testBalance = new TokenBalance
            {
                WalletAddress = "0xdef",
                TokenAddress = "0xtoken5",
                Balance = "2000",
                ChainType = "ETH",
                TokenName = "Token 5",
                TokenSymbol = "TK5",
                Decimals = 18,
                LastUpdated = System.DateTime.UtcNow.AddDays(-1)
            };

            await InsertTestTokenBalances(new List<TokenBalance> { testBalance });

            // Update the balance
            testBalance.Balance = "3000";
            testBalance.LastUpdated = System.DateTime.UtcNow;

            // Act
            await _tokenBalanceRepository.AddOrUpdateAsync(testBalance);

            // Assert
            var updatedBalance = await _tokenBalanceRepository.GetByWalletAndTokenAsync("0xdef", "0xtoken5", "ETH");
            Assert.IsNotNull(updatedBalance);
            Assert.AreEqual("3000", updatedBalance.Balance);
        }

        [TestMethod]
        public async Task DeleteForWalletAsync_ShouldRemoveAllTokenBalancesForWallet()
        {
            // Arrange
            var testBalances = new List<TokenBalance>
            {
                new TokenBalance
                {
                    WalletAddress = "0xghi",
                    TokenAddress = "0xtoken6",
                    Balance = "100",
                    ChainType = "ETH",
                    TokenName = "Token 6",
                    TokenSymbol = "TK6",
                    Decimals = 18,
                    LastUpdated = System.DateTime.UtcNow
                },
                new TokenBalance
                {
                    WalletAddress = "0xghi",
                    TokenAddress = "0xtoken7",
                    Balance = "200",
                    ChainType = "ETH",
                    TokenName = "Token 7",
                    TokenSymbol = "TK7",
                    Decimals = 18,
                    LastUpdated = System.DateTime.UtcNow
                }
            };

            await InsertTestTokenBalances(testBalances);

            // Act
            await _tokenBalanceRepository.DeleteForWalletAsync("0xghi");

            // Assert
            var remainingBalances = await _tokenBalanceRepository.GetAllForWalletAsync("0xghi");
            Assert.AreEqual(0, remainingBalances.Count());
        }
    }
}
