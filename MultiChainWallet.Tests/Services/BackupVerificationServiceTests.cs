using System;
using System.Threading.Tasks;
using MultiChainWallet.Core.Models;
using MultiChainWallet.Infrastructure.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using MultiChainWallet.Core.Interfaces;

namespace MultiChainWallet.Tests.Services
{
    /// <summary>
    /// 备份验证服务测试类
    /// Backup verification service test class
    /// </summary>
    [TestClass]
    public class BackupVerificationServiceTests
    {
        private BackupVerificationService _verificationService;
        private Mock<IPerformanceMonitorService> _mockPerformanceMonitor;

        [TestInitialize]
        public void Initialize()
        {
            _mockPerformanceMonitor = new Mock<IPerformanceMonitorService>();
            _verificationService = new BackupVerificationService(_mockPerformanceMonitor.Object);
        }

        [TestMethod]
        public async Task VerifyBackupData_WithValidData_ShouldReturnTrue()
        {
            // Arrange
            var backupData = new BackupData
            {
                WalletAccounts = new List<WalletAccount>
                {
                    new WalletAccount { Address = "0x123", PrivateKey = "test-key" }
                },
                TokenBalances = new List<TokenBalance>(),
                Transactions = new List<Transaction>(),
                CreatedAt = DateTime.UtcNow,
                Version = "1.0",
                Description = "Test backup",
                Checksum = "test-checksum"
            };

            // Act
            var result = await _verificationService.VerifyBackupDataAsync(backupData);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.ValidationErrors.Count);
        }

        [TestMethod]
        public async Task VerifyBackupData_WithInvalidData_ShouldReturnFalse()
        {
            // Arrange
            var backupData = new BackupData
            {
                WalletAccounts = null,
                TokenBalances = null,
                Transactions = null,
                CreatedAt = DateTime.MinValue,
                Version = "",
                Description = "",
                Checksum = ""
            };

            // Act
            var result = await _verificationService.VerifyBackupDataAsync(backupData);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreNotEqual(0, result.ValidationErrors.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task VerifyBackupData_WithNullData_ShouldThrowArgumentNullException()
        {
            // Act
            await _verificationService.VerifyBackupDataAsync(null);
        }

        [TestMethod]
        public async Task VerifyBackupData_ShouldTrackPerformance()
        {
            // Arrange
            var backupData = new BackupData
            {
                WalletAccounts = new List<WalletAccount>(),
                TokenBalances = new List<TokenBalance>(),
                Transactions = new List<Transaction>(),
                CreatedAt = DateTime.UtcNow,
                Version = "1.0",
                Description = "Test backup",
                Checksum = "test-checksum"
            };

            // Act
            await _verificationService.VerifyBackupDataAsync(backupData);

            // Assert
            _mockPerformanceMonitor.Verify(
                m => m.MonitorOperationAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<bool>>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task VerifyBackupData_WithInvalidWalletData_ShouldReturnSpecificError()
        {
            // Arrange
            var backupData = new BackupData
            {
                WalletAccounts = new List<WalletAccount>
                {
                    new WalletAccount { Address = "", PrivateKey = "" }
                },
                TokenBalances = new List<TokenBalance>(),
                Transactions = new List<Transaction>(),
                CreatedAt = DateTime.UtcNow,
                Version = "1.0",
                Description = "Test backup",
                Checksum = "test-checksum"
            };

            // Act
            var result = await _verificationService.VerifyBackupDataAsync(backupData);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ValidationErrors.Exists(error => error.Contains("wallet address")));
        }

        [TestMethod]
        public async Task VerifyBackupData_WithFutureDate_ShouldReturnError()
        {
            // Arrange
            var backupData = new BackupData
            {
                WalletAccounts = new List<WalletAccount>(),
                TokenBalances = new List<TokenBalance>(),
                Transactions = new List<Transaction>(),
                CreatedAt = DateTime.UtcNow.AddDays(1),
                Version = "1.0",
                Description = "Test backup",
                Checksum = "test-checksum"
            };

            // Act
            var result = await _verificationService.VerifyBackupDataAsync(backupData);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ValidationErrors.Exists(error => error.Contains("future date")));
        }
    }
}
