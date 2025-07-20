using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiChainWallet.Core.Models;
using MultiChainWallet.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MultiChainWallet.Tests.Data
{
    [TestClass]
    public class TransactionRepositoryTests
    {
        private string _connectionString;
        private TransactionRepository _transactionRepository;

        [TestInitialize]
        public async Task Initialize()
        {
            // 使用内存数据库进行测试
            // Use in-memory database for testing
            _connectionString = "Data Source=:memory:";
            _transactionRepository = new TransactionRepository(_connectionString);

            // 创建测试数据库
            // Create test database
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // 创建交易表
                // Create transaction table
                var createTableCommand = connection.CreateCommand();
                createTableCommand.CommandText = @"
                    CREATE TABLE Transactions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Hash TEXT NOT NULL UNIQUE,
                        BlockNumber INTEGER,
                        Timestamp TEXT,
                        FromAddress TEXT NOT NULL,
                        ToAddress TEXT NOT NULL,
                        Value TEXT,
                        GasPrice TEXT,
                        GasLimit TEXT,
                        GasUsed TEXT,
                        Nonce INTEGER,
                        IsSuccess INTEGER NOT NULL,
                        ChainType TEXT NOT NULL,
                        TokenContractAddress TEXT,
                        TokenValue TEXT,
                        TokenSymbol TEXT,
                        TokenDecimals INTEGER,
                        TransactionType INTEGER NOT NULL,
                        Status INTEGER NOT NULL,
                        Note TEXT
                    )";
                await createTableCommand.ExecuteNonQueryAsync();
            }
        }

        private async Task InsertTestTransactions(List<Transaction> transactions)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                foreach (var tx in transactions)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT INTO Transactions (Hash, BlockNumber, Timestamp, FromAddress, ToAddress, Value, GasPrice, GasLimit, GasUsed, Nonce, IsSuccess, ChainType, TokenContractAddress, TokenValue, TokenSymbol, TokenDecimals, TransactionType, Status, Note)
                        VALUES (@Hash, @BlockNumber, @Timestamp, @FromAddress, @ToAddress, @Value, @GasPrice, @GasLimit, @GasUsed, @Nonce, @IsSuccess, @ChainType, @TokenContractAddress, @TokenValue, @TokenSymbol, @TokenDecimals, @TransactionType, @Status, @Note)";
                    command.Parameters.AddWithValue("@Hash", tx.Hash);
                    command.Parameters.AddWithValue("@BlockNumber", tx.BlockNumber);
                    command.Parameters.AddWithValue("@Timestamp", tx.Timestamp?.ToString("o"));
                    command.Parameters.AddWithValue("@FromAddress", tx.FromAddress);
                    command.Parameters.AddWithValue("@ToAddress", tx.ToAddress);
                    command.Parameters.AddWithValue("@Value", tx.Value);
                    command.Parameters.AddWithValue("@GasPrice", tx.GasPrice);
                    command.Parameters.AddWithValue("@GasLimit", tx.GasLimit);
                    command.Parameters.AddWithValue("@GasUsed", tx.GasUsed);
                    command.Parameters.AddWithValue("@Nonce", tx.Nonce);
                    command.Parameters.AddWithValue("@IsSuccess", tx.IsSuccess ? 1 : 0);
                    command.Parameters.AddWithValue("@ChainType", tx.ChainType);
                    command.Parameters.AddWithValue("@TokenContractAddress", tx.TokenContractAddress);
                    command.Parameters.AddWithValue("@TokenValue", tx.TokenValue);
                    command.Parameters.AddWithValue("@TokenSymbol", tx.TokenSymbol);
                    command.Parameters.AddWithValue("@TokenDecimals", tx.TokenDecimals);
                    command.Parameters.AddWithValue("@TransactionType", (int)tx.TransactionType);
                    command.Parameters.AddWithValue("@Status", (int)tx.Status);
                    command.Parameters.AddWithValue("@Note", tx.Note);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        [TestMethod]
        public async Task GetAllForAddressAsync_ShouldReturnAllTransactionsForAddress()
        {
            // Arrange
            var testTransactions = new List<Transaction>
            {
                new Transaction
                {
                    Hash = "0xtx1",
                    BlockNumber = 1000,
                    Timestamp = DateTime.UtcNow.AddDays(-1),
                    FromAddress = "0x123",
                    ToAddress = "0x456",
                    Value = "1.0",
                    GasPrice = "20",
                    GasLimit = "21000",
                    GasUsed = "21000",
                    Nonce = 1,
                    IsSuccess = true,
                    ChainType = "ETH",
                    TransactionType = TransactionType.Transfer,
                    Status = TransactionStatus.Confirmed,
                    Note = "Test transaction 1"
                },
                new Transaction
                {
                    Hash = "0xtx2",
                    BlockNumber = 1001,
                    Timestamp = DateTime.UtcNow.AddHours(-12),
                    FromAddress = "0x123",
                    ToAddress = "0x789",
                    Value = "0.5",
                    GasPrice = "20",
                    GasLimit = "21000",
                    GasUsed = "21000",
                    Nonce = 2,
                    IsSuccess = true,
                    ChainType = "ETH",
                    TransactionType = TransactionType.Transfer,
                    Status = TransactionStatus.Confirmed,
                    Note = "Test transaction 2"
                },
                new Transaction
                {
                    Hash = "0xtx3",
                    BlockNumber = 1002,
                    Timestamp = DateTime.UtcNow.AddHours(-6),
                    FromAddress = "0x789",
                    ToAddress = "0x123",
                    Value = "0.25",
                    GasPrice = "20",
                    GasLimit = "21000",
                    GasUsed = "21000",
                    Nonce = 1,
                    IsSuccess = true,
                    ChainType = "ETH",
                    TransactionType = TransactionType.Transfer,
                    Status = TransactionStatus.Confirmed,
                    Note = "Test transaction 3"
                }
            };

            await InsertTestTransactions(testTransactions);

            // Act
            var result = await _transactionRepository.GetAllForAddressAsync("0x123");

            // Assert
            Assert.AreEqual(3, result.Count());
            Assert.IsTrue(result.Any(t => t.Hash == "0xtx1"));
            Assert.IsTrue(result.Any(t => t.Hash == "0xtx2"));
            Assert.IsTrue(result.Any(t => t.Hash == "0xtx3"));
        }

        [TestMethod]
        public async Task GetByHashAsync_WithExistingTransaction_ShouldReturnTransaction()
        {
            // Arrange
            var testTransaction = new Transaction
            {
                Hash = "0xtx4",
                BlockNumber = 1003,
                Timestamp = DateTime.UtcNow.AddHours(-3),
                FromAddress = "0xabc",
                ToAddress = "0xdef",
                Value = "2.0",
                GasPrice = "20",
                GasLimit = "21000",
                GasUsed = "21000",
                Nonce = 1,
                IsSuccess = true,
                ChainType = "ETH",
                TransactionType = TransactionType.Transfer,
                Status = TransactionStatus.Confirmed,
                Note = "Test transaction 4"
            };

            await InsertTestTransactions(new List<Transaction> { testTransaction });

            // Act
            var result = await _transactionRepository.GetByHashAsync("0xtx4");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("0xabc", result.FromAddress);
            Assert.AreEqual("0xdef", result.ToAddress);
            Assert.AreEqual("2.0", result.Value);
        }

        [TestMethod]
        public async Task GetByHashAsync_WithNonExistingTransaction_ShouldReturnNull()
        {
            // Act
            var result = await _transactionRepository.GetByHashAsync("0xnonexistent");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task AddAsync_ShouldAddTransactionToDatabase()
        {
            // Arrange
            var newTransaction = new Transaction
            {
                Hash = "0xtx5",
                BlockNumber = 1004,
                Timestamp = DateTime.UtcNow.AddHours(-1),
                FromAddress = "0xghi",
                ToAddress = "0xjkl",
                Value = "3.0",
                GasPrice = "20",
                GasLimit = "21000",
                GasUsed = "21000",
                Nonce = 1,
                IsSuccess = true,
                ChainType = "ETH",
                TransactionType = TransactionType.Transfer,
                Status = TransactionStatus.Confirmed,
                Note = "Test transaction 5"
            };

            // Act
            await _transactionRepository.AddAsync(newTransaction);

            // Assert
            var addedTransaction = await _transactionRepository.GetByHashAsync("0xtx5");
            Assert.IsNotNull(addedTransaction);
            Assert.AreEqual("0xghi", addedTransaction.FromAddress);
            Assert.AreEqual("3.0", addedTransaction.Value);
        }

        [TestMethod]
        public async Task UpdateAsync_ShouldUpdateTransactionInDatabase()
        {
            // Arrange
            var testTransaction = new Transaction
            {
                Hash = "0xtx6",
                BlockNumber = 1005,
                Timestamp = DateTime.UtcNow.AddMinutes(-30),
                FromAddress = "0xmno",
                ToAddress = "0xpqr",
                Value = "4.0",
                GasPrice = "20",
                GasLimit = "21000",
                GasUsed = "21000",
                Nonce = 1,
                IsSuccess = true,
                ChainType = "ETH",
                TransactionType = TransactionType.Transfer,
                Status = TransactionStatus.Pending,
                Note = "Test transaction 6"
            };

            await InsertTestTransactions(new List<Transaction> { testTransaction });

            // Update the transaction
            testTransaction.BlockNumber = 1006;
            testTransaction.Status = TransactionStatus.Confirmed;
            testTransaction.Note = "Updated transaction 6";

            // Act
            await _transactionRepository.UpdateAsync(testTransaction);

            // Assert
            var updatedTransaction = await _transactionRepository.GetByHashAsync("0xtx6");
            Assert.IsNotNull(updatedTransaction);
            Assert.AreEqual(1006, updatedTransaction.BlockNumber);
            Assert.AreEqual(TransactionStatus.Confirmed, updatedTransaction.Status);
            Assert.AreEqual("Updated transaction 6", updatedTransaction.Note);
        }
    }
}
