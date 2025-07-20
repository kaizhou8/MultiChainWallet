using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Interfaces;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 性能监控服务
    /// Performance monitoring service
    /// </summary>
    public class PerformanceMonitorService : IPerformanceMonitorService
    {
        private readonly ILogger<PerformanceMonitorService> _logger;

        public PerformanceMonitorService(ILogger<PerformanceMonitorService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 监控异步操作的性能
        /// Monitor performance of async operation
        /// </summary>
        public async Task<T> MonitorOperationAsync<T>(string operationName, Func<Task<T>> operation)
        {
            var stopwatch = new Stopwatch();
            var startMemory = GC.GetTotalMemory(false);

            try
            {
                stopwatch.Start();
                var result = await operation();
                stopwatch.Stop();

                var endMemory = GC.GetTotalMemory(false);
                var memoryUsed = endMemory - startMemory;

                _logger.LogInformation(
                    "Operation {OperationName} completed in {ElapsedMilliseconds}ms, Memory used: {MemoryUsed} bytes",
                    operationName,
                    stopwatch.ElapsedMilliseconds,
                    memoryUsed);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(
                    ex,
                    "Operation {OperationName} failed after {ElapsedMilliseconds}ms",
                    operationName,
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
