using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MultiChainWallet.Infrastructure.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MultiChainWallet.Tests.Services
{
    /// <summary>
    /// 性能监控服务测试类
    /// Performance monitor service test class
    /// </summary>
    [TestClass]
    public class PerformanceMonitorServiceTests
    {
        private PerformanceMonitorService _performanceMonitor;

        [TestInitialize]
        public void Initialize()
        {
            _performanceMonitor = new PerformanceMonitorService();
        }

        [TestMethod]
        public async Task MonitorOperation_ShouldTrackExecutionTime()
        {
            // Arrange
            string operationName = "TestOperation";
            int delay = 100;

            // Act
            await _performanceMonitor.MonitorOperationAsync(operationName, async () =>
            {
                await Task.Delay(delay);
            });

            // Assert
            var stats = _performanceMonitor.GetOperationStats(operationName);
            Assert.IsNotNull(stats);
            Assert.IsTrue(stats.AverageExecutionTime >= delay);
        }

        [TestMethod]
        public async Task MonitorOperation_WithReturnValue_ShouldReturnCorrectValue()
        {
            // Arrange
            string operationName = "TestOperationWithReturn";
            string expectedResult = "test result";

            // Act
            var result = await _performanceMonitor.MonitorOperationAsync(operationName, async () =>
            {
                await Task.Delay(10);
                return expectedResult;
            });

            // Assert
            Assert.AreEqual(expectedResult, result);
            var stats = _performanceMonitor.GetOperationStats(operationName);
            Assert.IsNotNull(stats);
        }

        [TestMethod]
        public async Task MonitorOperation_ShouldTrackMemoryUsage()
        {
            // Arrange
            string operationName = "TestMemoryOperation";
            var largeObject = new byte[1024 * 1024]; // 1MB

            // Act
            await _performanceMonitor.MonitorOperationAsync(operationName, async () =>
            {
                await Task.Delay(10);
                GC.KeepAlive(largeObject);
            });

            // Assert
            var stats = _performanceMonitor.GetOperationStats(operationName);
            Assert.IsNotNull(stats);
            Assert.IsTrue(stats.AverageMemoryDelta != 0);
        }

        [TestMethod]
        public async Task GetOperationStats_ShouldReturnCorrectStats()
        {
            // Arrange
            string operationName = "TestStatsOperation";
            int executionCount = 5;

            // Act
            for (int i = 0; i < executionCount; i++)
            {
                await _performanceMonitor.MonitorOperationAsync(operationName, async () =>
                {
                    await Task.Delay(10);
                });
            }

            // Assert
            var stats = _performanceMonitor.GetOperationStats(operationName);
            Assert.IsNotNull(stats);
            Assert.AreEqual(operationName, stats.OperationName);
            Assert.AreEqual(executionCount, stats.TotalOperations);
            Assert.IsTrue(stats.MinExecutionTime <= stats.MaxExecutionTime);
        }

        [TestMethod]
        public async Task GetAllOperationStats_ShouldReturnAllStats()
        {
            // Arrange
            var operations = new[] { "Operation1", "Operation2", "Operation3" };

            // Act
            foreach (var operation in operations)
            {
                await _performanceMonitor.MonitorOperationAsync(operation, async () =>
                {
                    await Task.Delay(10);
                });
            }

            // Assert
            var allStats = _performanceMonitor.GetAllOperationStats();
            Assert.AreEqual(operations.Length, allStats.Count);
            Assert.All(operations, op => Assert.Contains(allStats, s => s.OperationName == op));
        }

        [TestMethod]
        public async Task CleanupOldMetrics_ShouldRemoveOldData()
        {
            // Arrange
            string operationName = "TestCleanupOperation";
            await _performanceMonitor.MonitorOperationAsync(operationName, async () =>
            {
                await Task.Delay(10);
            });

            // Act
            _performanceMonitor.CleanupOldMetrics(TimeSpan.FromMilliseconds(1));
            await Task.Delay(10); // Wait for cleanup

            // Assert
            var stats = _performanceMonitor.GetOperationStats(operationName);
            Assert.IsNotNull(stats);
            Assert.IsTrue(stats.TotalOperations <= 1);
        }

        [TestMethod]
        public async Task MonitorOperation_WithException_ShouldStillRecordMetrics()
        {
            // Arrange
            string operationName = "TestExceptionOperation";

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
            {
                await _performanceMonitor.MonitorOperationAsync(operationName, async () =>
                {
                    await Task.Delay(10);
                    throw new InvalidOperationException("Test exception");
                });
            });

            var stats = _performanceMonitor.GetOperationStats(operationName);
            Assert.IsNotNull(stats);
            Assert.AreEqual(1, stats.TotalOperations);
        }
    }
}
