using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MultiChainWallet.Core.Utilities
{
    /// <summary>
    /// 应用程序启动优化器，用于减少混淆对启动性能的影响
    /// Application startup optimizer to reduce the impact of obfuscation on startup performance
    /// </summary>
    public static class AppStartupOptimizer
    {
        private static bool _isInitialized = false;
        private static readonly object _lockObject = new object();
        
        /// <summary>
        /// 初始化优化器，预热关键组件
        /// Initialize optimizer and warm up critical components
        /// </summary>
        /// <param name="assemblies">要优化的程序集数组，如果为null则使用当前应用程序域中的所有程序集 / Array of assemblies to optimize, if null uses all assemblies in current AppDomain</param>
        /// <returns>预热任务 / Warm-up task</returns>
        public static Task InitializeAsync(Assembly[] assemblies = null)
        {
            if (_isInitialized)
                return Task.CompletedTask;
                
            return Task.Run(() => 
            {
                lock (_lockObject)
                {
                    if (_isInitialized)
                        return;
                        
                    try
                    {
                        Stopwatch sw = Stopwatch.StartNew();
                        
                        // 如果未指定程序集，使用当前应用程序域中的所有程序集
                        // If no assemblies specified, use all assemblies in current AppDomain
                        if (assemblies == null || assemblies.Length == 0)
                        {
                            assemblies = AppDomain.CurrentDomain.GetAssemblies()
                                .Where(a => !a.IsDynamic && !a.GlobalAssemblyCache)
                                .ToArray();
                        }
                        
                        // 预热关键类型的反射缓存
                        // Warm up reflection cache for critical types
                        WarmUpReflectionCache(assemblies);
                        
                        // 预加载常用类型
                        // Preload commonly used types
                        PreloadCommonTypes(assemblies);
                        
                        sw.Stop();
                        Debug.WriteLine($"应用程序启动优化完成，耗时: {sw.ElapsedMilliseconds}ms / Application startup optimization completed, time taken: {sw.ElapsedMilliseconds}ms");
                        
                        _isInitialized = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"应用程序启动优化失败: {ex.Message} / Application startup optimization failed: {ex.Message}");
                    }
                }
            });
        }
        
        /// <summary>
        /// 预热反射缓存
        /// Warm up reflection cache
        /// </summary>
        /// <param name="assemblies">程序集数组 / Array of assemblies</param>
        private static void WarmUpReflectionCache(Assembly[] assemblies)
        {
            // 定义关键类型模式
            // Define critical type patterns
            string[] criticalTypePatterns = new[]
            {
                "Wallet",
                "Transaction",
                "Balance",
                "Security",
                "Crypto",
                "Service",
                "Repository",
                "ViewModel",
                "Model",
                "Factory",
                "Provider",
                "Manager"
            };
            
            foreach (var assembly in assemblies)
            {
                if (assembly.FullName.StartsWith("MultiChainWallet"))
                {
                    try
                    {
                        // 使用ReflectionHelper预热缓存
                        // Use ReflectionHelper to warm up cache
                        ReflectionHelper.WarmUpCache(assembly, criticalTypePatterns);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"预热程序集 {assembly.FullName} 的反射缓存时出错: {ex.Message} / Error warming up reflection cache for assembly {assembly.FullName}: {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 预加载常用类型
        /// Preload commonly used types
        /// </summary>
        /// <param name="assemblies">程序集数组 / Array of assemblies</param>
        private static void PreloadCommonTypes(Assembly[] assemblies)
        {
            // 定义要预加载的类型名称
            // Define type names to preload
            var typeNamesToPreload = new List<string>
            {
                // 核心服务
                // Core services
                "MultiChainWallet.Core.Services.WalletService",
                "MultiChainWallet.Core.Services.TransactionService",
                "MultiChainWallet.Core.Services.BalanceService",
                "MultiChainWallet.Core.Services.SecurityService",
                
                // 数据模型
                // Data models
                "MultiChainWallet.Core.Models.Wallet",
                "MultiChainWallet.Core.Models.Transaction",
                "MultiChainWallet.Core.Models.Balance",
                
                // 接口
                // Interfaces
                "MultiChainWallet.Core.Interfaces.IWalletService",
                "MultiChainWallet.Core.Interfaces.ITransactionService",
                "MultiChainWallet.Core.Interfaces.IBalanceService",
                "MultiChainWallet.Core.Interfaces.ISecurityService",
                
                // 视图模型
                // View models
                "MultiChainWallet.UI.ViewModels.WalletViewModel",
                "MultiChainWallet.UI.ViewModels.TransactionViewModel",
                "MultiChainWallet.UI.ViewModels.MainViewModel"
            };
            
            foreach (var typeName in typeNamesToPreload)
            {
                try
                {
                    // 尝试加载类型
                    // Try to load type
                    var type = ReflectionHelper.GetType(typeName);
                    
                    if (type != null)
                    {
                        // 预加载公共方法和属性
                        // Preload public methods and properties
                        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                        
                        // 触发JIT编译
                        // Trigger JIT compilation
                        foreach (var method in methods)
                        {
                            if (!method.IsAbstract && !method.ContainsGenericParameters && method.GetParameters().Length == 0)
                            {
                                try
                                {
                                    // 获取方法句柄以触发JIT编译
                                    // Get method handle to trigger JIT compilation
                                    RuntimeHelpers.PrepareMethod(method.MethodHandle);
                                }
                                catch
                                {
                                    // 忽略错误
                                    // Ignore errors
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"预加载类型 {typeName} 时出错: {ex.Message} / Error preloading type {typeName}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 在应用程序启动时调用此方法以优化性能
        /// Call this method at application startup to optimize performance
        /// </summary>
        public static void OptimizeStartup()
        {
            // 启动异步初始化
            // Start async initialization
            var initTask = InitializeAsync();
            
            // 不等待任务完成，让它在后台运行
            // Don't wait for task completion, let it run in background
        }
    }
} 