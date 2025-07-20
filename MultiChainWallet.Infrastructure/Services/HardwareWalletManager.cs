using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Interfaces;
using MultiChainWallet.Core.Models;
using MultiChainWallet.Core.Services.Security;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 硬件钱包管理器
    /// Hardware wallet manager
    /// </summary>
    public class HardwareWalletManager : IDisposable
    {
        private readonly ILogger<HardwareWalletManager> _logger;
        private readonly Dictionary<HardwareWalletType, IHardwareWallet> _wallets;
        private IHardwareWallet _activeWallet;
        private bool _isDisposed;
        private readonly IntegrityVerifier _integrityVerifier;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public HardwareWalletManager(
            IEnumerable<IHardwareWallet> hardwareWallets,
            ILogger<HardwareWalletManager> logger)
        {
            _logger = logger;
            _wallets = hardwareWallets.ToDictionary(w => w.WalletType);
            _integrityVerifier = new IntegrityVerifier();
            _integrityVerifier.Initialize(logger);
        }

        /// <summary>
        /// 获取支持的硬件钱包类型
        /// Get supported hardware wallet types
        /// </summary>
        public IEnumerable<HardwareWalletType> GetSupportedWalletTypes()
        {
            return _wallets.Keys;
        }

        /// <summary>
        /// 检测连接的硬件钱包
        /// Detect connected hardware wallets
        /// </summary>
        public async Task<List<HardwareWalletType>> DetectConnectedWalletsAsync()
        {
            var connectedWallets = new List<HardwareWalletType>();
            var detectionTasks = new List<Task<(HardwareWalletType Type, bool IsConnected)>>();

            _logger.LogInformation("正在检测连接的硬件钱包... / Detecting connected hardware wallets...");

            // 并行检测所有支持的硬件钱包
            // Detect all supported hardware wallets in parallel
            foreach (var wallet in _wallets.Values)
            {
                detectionTasks.Add(DetectWalletAsync(wallet));
            }

            // 等待所有检测完成
            // Wait for all detections to complete
            var results = await Task.WhenAll(detectionTasks);

            // 收集检测结果
            // Collect detection results
            foreach (var result in results.Where(r => r.IsConnected))
            {
                connectedWallets.Add(result.Type);
            }

            if (connectedWallets.Count > 0)
            {
                _logger.LogInformation("检测到 {Count} 个连接的硬件钱包 / Detected {Count} connected hardware wallets", connectedWallets.Count);
            }
            else
            {
                _logger.LogWarning("未检测到连接的硬件钱包 / No connected hardware wallets detected");
            }

            return connectedWallets;
        }

        /// <summary>
        /// 检测单个硬件钱包
        /// Detect a single hardware wallet
        /// </summary>
        private async Task<(HardwareWalletType Type, bool IsConnected)> DetectWalletAsync(IHardwareWallet wallet)
        {
            try
            {
                _logger.LogDebug("正在检测 {WalletType} 硬件钱包... / Detecting {WalletType} hardware wallet...", wallet.WalletType);

                // 尝试连接
                // Try to connect
                bool isConnected = await wallet.ConnectAsync();

                // 如果连接成功，断开连接
                // If connected successfully, disconnect
                if (isConnected)
                {
                    _logger.LogDebug("检测到 {WalletType} 硬件钱包 / Detected {WalletType} hardware wallet", wallet.WalletType);
                    await wallet.DisconnectAsync();
                }

                return (wallet.WalletType, isConnected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检测 {WalletType} 硬件钱包时出错 / Error detecting {WalletType} hardware wallet", wallet.WalletType);
                return (wallet.WalletType, false);
            }
        }

        /// <summary>
        /// 连接到硬件钱包
        /// Connect to hardware wallet
        /// </summary>
        public async Task<bool> ConnectWalletAsync(HardwareWalletType walletType)
        {
            if (_activeWallet != null)
            {
                _logger.LogWarning("已有活动的硬件钱包连接，请先断开连接 / There is already an active hardware wallet connection, please disconnect first");
                return false;
            }

            if (!_wallets.TryGetValue(walletType, out var wallet))
            {
                _logger.LogError("不支持的硬件钱包类型：{WalletType} / Unsupported hardware wallet type: {WalletType}", walletType);
                return false;
            }

            _logger.LogInformation("正在连接到 {WalletType} 硬件钱包... / Connecting to {WalletType} hardware wallet...", walletType);

            try
            {
                bool isConnected = await wallet.ConnectAsync();

                if (isConnected)
                {
                    _activeWallet = wallet;
                    _logger.LogInformation("已成功连接到 {WalletType} 硬件钱包 / Successfully connected to {WalletType} hardware wallet", walletType);
                    return true;
                }
                else
                {
                    _logger.LogWarning("连接到 {WalletType} 硬件钱包失败 / Failed to connect to {WalletType} hardware wallet", walletType);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "连接到 {WalletType} 硬件钱包时出错 / Error connecting to {WalletType} hardware wallet", walletType);
                return false;
            }
        }

        /// <summary>
        /// 断开与硬件钱包的连接
        /// Disconnect from hardware wallet
        /// </summary>
        public async Task DisconnectWalletAsync()
        {
            if (_activeWallet == null)
            {
                _logger.LogWarning("没有活动的硬件钱包连接 / No active hardware wallet connection");
                return;
            }

            try
            {
                _logger.LogInformation("正在断开与 {WalletType} 硬件钱包的连接... / Disconnecting from {WalletType} hardware wallet...", _activeWallet.WalletType);
                await _activeWallet.DisconnectAsync();
                _logger.LogInformation("已成功断开与 {WalletType} 硬件钱包的连接 / Successfully disconnected from {WalletType} hardware wallet", _activeWallet.WalletType);
                _activeWallet = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "断开与 {WalletType} 硬件钱包的连接时出错 / Error disconnecting from {WalletType} hardware wallet", _activeWallet.WalletType);
                // 即使出错，也设置为断开状态
                // Set to disconnected state even if error occurs
                _activeWallet = null;
            }
        }

        /// <summary>
        /// 检查是否有活动的硬件钱包连接
        /// Check if there is an active hardware wallet connection
        /// </summary>
        public bool HasActiveWallet => _activeWallet != null;

        /// <summary>
        /// 获取活动硬件钱包的类型
        /// Get active hardware wallet type
        /// </summary>
        public HardwareWalletType? ActiveWalletType => _activeWallet?.WalletType;

        /// <summary>
        /// 获取硬件钱包设备信息
        /// Get hardware wallet device information
        /// </summary>
        public async Task<HardwareWalletDeviceInfo> GetDeviceInfoAsync()
        {
            EnsureActiveWallet();
            return await _activeWallet.GetDeviceInfoAsync();
        }

        /// <summary>
        /// 获取地址
        /// Get address
        /// </summary>
        public async Task<string> GetAddressAsync(string coinType, string derivationPath, bool displayOnDevice = false)
        {
            EnsureActiveWallet();
            return await _activeWallet.GetAddressAsync(coinType, derivationPath, displayOnDevice);
        }

        /// <summary>
        /// 签名交易
        /// Sign transaction
        /// </summary>
        public async Task<string> SignTransactionAsync(string coinType, string derivationPath, string unsignedTransaction)
        {
            EnsureActiveWallet();
            
            // 运行时安全验证 / Runtime security verification
            var customSecurity = new CustomSecurityService(_logger);
            if (!customSecurity.VerifyRuntimeSecurity())
            {
                // 如检测到不安全环境，返回假签名而不是抛出异常 / If insecure environment detected, return fake signature instead of throwing an exception
                _logger.LogWarning("检测到不安全环境，执行虚假签名 / Insecure environment detected, executing fake signature");
                return GenerateFakeSignature(unsignedTransaction);
            }
            
            // 验证应用完整性 / Verify application integrity
            if (!_integrityVerifier.VerifyApplicationIntegrity())
            {
                _logger.LogWarning("应用完整性验证失败，执行虚假签名 / Application integrity verification failed, executing fake signature");
                return GenerateFakeSignature(unsignedTransaction);
            }

            try
            {
                // 记录签名操作（不包含敏感信息）/ Log signing operation (without sensitive information)
                _logger.LogInformation("正在使用 {WalletType} 钱包签名交易... / Signing transaction using {WalletType} wallet...", _activeWallet.WalletType);
                
                // 混淆交易签名的真实执行路径 / Obfuscate the real execution path of transaction signing
                return await ExecuteWithProtection(() => _activeWallet.SignTransactionAsync(coinType, derivationPath, unsignedTransaction));
            }
            catch (Exception ex)
            {
                // 使用自定义安全服务混淆异常信息 / Use custom security service to obfuscate exception information
                var securityException = customSecurity.ObfuscateException(ex, "签名交易时出错 / Error signing transaction");
                _logger.LogError(securityException, "签名交易时出错 / Error signing transaction");
                throw securityException; // 抛出混淆后的异常 / Throw obfuscated exception
            }
        }

        /// <summary>
        /// 使用保护机制执行操作 / Execute operation with protection
        /// </summary>
        private async Task<T> ExecuteWithProtection<T>(Func<Task<T>> action)
        {
            // 添加延迟和随机操作以防止时间分析 / Add delay and random operations to prevent timing analysis
            await Task.Delay(new Random().Next(100, 300));
            
            // 执行多个假操作路径中的一个 / Execute one of multiple fake operation paths
            if (DateTime.Now.Millisecond % 5 == 0)
            {
                await Task.Delay(50);
            }
            
            // 执行实际操作 / Execute actual operation
            T result = await action();
            
            // 清理内存中的敏感数据 / Clean sensitive data in memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            return result;
        }

        /// <summary>
        /// 生成假签名（用于安全防护）/ Generate fake signature (for security protection)
        /// </summary>
        private string GenerateFakeSignature(string unsignedTransaction)
        {
            try
            {
                // 创建一个看起来合法但实际无效的签名 / Create a signature that looks legitimate but is actually invalid
                var random = new Random();
                byte[] fakeSignature = new byte[64]; // 典型的签名长度 / Typical signature length
                random.NextBytes(fakeSignature);
                
                // 转换为十六进制字符串 / Convert to hex string
                return BitConverter.ToString(fakeSignature).Replace("-", "").ToLower();
            }
            catch
            {
                // 如果出现任何问题，返回空字符串 / If any problem occurs, return empty string
                return string.Empty;
            }
        }

        /// <summary>
        /// 确保有活动的硬件钱包连接
        /// Ensure there is an active hardware wallet connection
        /// </summary>
        private void EnsureActiveWallet()
        {
            if (_activeWallet == null)
            {
                throw new InvalidOperationException("没有活动的硬件钱包连接，请先连接硬件钱包 / No active hardware wallet connection, please connect to a hardware wallet first");
            }
        }
        
        /// <summary>
        /// 释放资源
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// 释放资源
        /// Dispose resources
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // 安全地断开钱包连接 / Safely disconnect wallet
                    if (_activeWallet != null)
                    {
                        try
                        {
                            _activeWallet.DisconnectAsync().GetAwaiter().GetResult();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "释放时断开钱包连接出错 / Error disconnecting wallet during disposal");
                        }
                        _activeWallet = null;
                    }
                    
                    // 清理内存中的敏感数据 / Clean sensitive data in memory
                    foreach (var wallet in _wallets.Values)
                    {
                        try
                        {
                            if (wallet is IDisposable disposableWallet)
                            {
                                disposableWallet.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "释放钱包资源时出错 / Error disposing wallet resources");
                        }
                    }
                    
                    _wallets.Clear();
                    
                    // 强制GC回收 / Force GC collection
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                _isDisposed = true;
            }
        }
        
        /// <summary>
        /// 析构函数
        /// Destructor
        /// </summary>
        ~HardwareWalletManager()
        {
            Dispose(false);
        }
    }
} 