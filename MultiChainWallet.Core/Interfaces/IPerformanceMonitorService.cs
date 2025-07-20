using System;
using System.Threading.Tasks;

namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// 性能监控服务接口
    /// Performance monitoring service interface
    /// </summary>
    public interface IPerformanceMonitorService
    {
        /// <summary>
        /// 监控异步操作
        /// Monitor async operation
        /// </summary>
        /// <typeparam name="T">操作返回类型 / Operation return type</typeparam>
        /// <param name="operationName">操作名称 / Operation name</param>
        /// <param name="operation">要执行的操作 / Operation to execute</param>
        /// <returns>操作结果 / Operation result</returns>
        Task<T> MonitorOperationAsync<T>(string operationName, Func<Task<T>> operation);
    }
}
