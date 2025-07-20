using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using MultiChainWallet.Core.Models;
using MultiChainWallet.Infrastructure.Data;
using MultiChainWallet.Infrastructure.Services;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MultiChainWallet.Tests.Services
{
    /// <summary>
    /// 备份服务测试类
    /// Backup service test class
    /// </summary>
    [TestClass]
    public class BackupServiceTests
    {
        private Mock<WalletRepository> _mockWalletRepo;
        private Mock<TransactionRepository> _mockTransactionRepo;
        private Mock<TokenBalanceRepository> _mockTokenBalanceRepo;
        private BackupService _backupService;
        private string _testDataDirectory;

        [TestInitialize]
        public void Initialize()
        {
            _mockWalletRepo = new Mock<WalletRepository>();
            _mockTransactionRepo = new Mock<TransactionRepository>();
            _mockTokenBalanceRepo = new Mock<TokenBalanceRepository>();

            _backupService = new BackupService(
                _mockWalletRepo.Object,
                _mockTransactionRepo.Object,
                _mockTokenBalanceRepo.Object);

            _testDataDirectory = Path.Combine(Path.GetTempPath(), "MultiChainWalletBackupTests");
            Directory.CreateDirectory(_testDataDirectory);
        }

        [TestMethod]
        public async Task CreateBackup_WithValidData_ShouldCreateBackupFile()
        {
            // Arrange
            var testWallet = new WalletAccount
            {
                Address = "0x123",
                PrivateKey = "test-private-key"
            };

            _mockWalletRepo.Setup(r => r.GetAllWalletsAsync())
                .ReturnsAsync(new List<WalletAccount> { testWallet });

            _mockTokenBalanceRepo.Setup(r => r.GetWalletTokenBalancesAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<TokenBalance>());

            _mockTransactionRepo.Setup(r => r.GetWalletTransactionsAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<Transaction>());

            // Act
            var backupPath = await _backupService.CreateBackupAsync("Valid@Password123");

            // Assert
            Assert.IsTrue(File.Exists(backupPath));
            Assert.IsTrue(new FileInfo(backupPath).Length > 0);
        }

        [TestMethod]
        public async Task CreateBackup_WithInvalidPassword_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _backupService.CreateBackupAsync("weak"));
        }

        [TestMethod]
        public async Task RestoreBackup_WithValidBackup_ShouldRestoreData()
        {
            // Arrange
            var testWallet = new WalletAccount
            {
                Address = "0x123",
                PrivateKey = "test-private-key"
            };

            _mockWalletRepo.Setup(r => r.GetAllWalletsAsync())
                .ReturnsAsync(new List<WalletAccount> { testWallet });

            _mockWalletRepo.Setup(r => r.WalletExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            var password = "Valid@Password123";
            var backupPath = await _backupService.CreateBackupAsync(password);

            // Act
            await _backupService.RestoreFromBackupAsync(backupPath, password);

            // Assert
            _mockWalletRepo.Verify(r => r.AddWalletAsync(It.IsAny<WalletAccount>()), Times.Once);
        }

        [TestMethod]
        public async Task RestoreBackup_WithInvalidPassword_ShouldThrowException()
        {
            // Arrange
            var testWallet = new WalletAccount
            {
                Address = "0x123",
                PrivateKey = "test-private-key"
            };

            _mockWalletRepo.Setup(r => r.GetAllWalletsAsync())
                .ReturnsAsync(new List<WalletAccount> { testWallet });

            var correctPassword = "Valid@Password123";
            var wrongPassword = "Wrong@Password123";
            var backupPath = await _backupService.CreateBackupAsync(correctPassword);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _backupService.RestoreFromBackupAsync(backupPath, wrongPassword));
        }

        [TestMethod]
        public async Task GetBackupInfo_ShouldReturnCorrectInfo()
        {
            // Arrange
            var testWallet = new WalletAccount
            {
                Address = "0x123",
                PrivateKey = "test-private-key"
            };

            _mockWalletRepo.Setup(r => r.GetAllWalletsAsync())
                .ReturnsAsync(new List<WalletAccount> { testWallet });

            var password = "Valid@Password123";
            var backupPath = await _backupService.CreateBackupAsync(password);

            // Act
            var backupInfo = await _backupService.GetBackupInfoAsync(backupPath);

            // Assert
            Assert.IsNotNull(backupInfo);
            Assert.AreEqual(backupPath, backupInfo.FilePath);
            Assert.IsTrue(backupInfo.SizeInBytes > 0);
            Assert.IsTrue(backupInfo.IsCompressed);
            Assert.IsTrue(backupInfo.CompressionRatio > 0);
        }

        [TestMethod]
        public async Task GetPerformanceStats_ShouldReturnStats()
        {
            // Arrange
            var testWallet = new WalletAccount
            {
                Address = "0x123",
                PrivateKey = "test-private-key"
            };

            _mockWalletRepo.Setup(r => r.GetAllWalletsAsync())
                .ReturnsAsync(new List<WalletAccount> { testWallet });

            // Act
            await _backupService.CreateBackupAsync("Valid@Password123");
            var stats = _backupService.GetPerformanceStats();

            // Assert
            Assert.IsNotNull(stats);
            Assert.AreNotEqual(0, stats.Count);
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                Directory.Delete(_testDataDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
