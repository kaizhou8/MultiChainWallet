using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Interfaces;
using MultiChainWallet.Core.Models;
using System.Threading;
using Ledger.Net;
using Ledger.Net.Responses;
using Ledger.Net.Requests;
using Hid.Net;
using Hid.Net.Windows;
using Device.Net;
using System.Text;
using System.Numerics;
using Hardwarewallets.Net.AddressManagement;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// Ledger硬件钱包实现
    /// Ledger hardware wallet implementation
    /// </summary>
    public class LedgerHardwareWallet : IHardwareWallet, IDisposable
    {
        private readonly ILogger<LedgerHardwareWallet> _logger;
        private bool _isConnected;
        private LedgerManager _ledgerManager; // Ledger SDK管理器 / Ledger SDK manager
        private LedgerState? _currentState; // 当前Ledger状态 / Current Ledger state
        
        // 连接超时（毫秒）
        // Connection timeout (milliseconds)
        private const int CONNECTION_TIMEOUT_MS = 10000;
        
        // 最大重试次数
        // Maximum retry count
        private const int MAX_RETRY_COUNT = 3;
        
        // 重试延迟（毫秒）
        // Retry delay (milliseconds)
        private const int RETRY_DELAY_MS = 1000;
        
        // Ledger设备标识
        // Ledger device identifiers
        private static readonly int[] LEDGER_VENDOR_IDS = { 0x2c97, 0x2581 };
        
        // 设备锁，防止并发访问
        // Device lock, prevent concurrent access
        private readonly SemaphoreSlim _deviceLock = new SemaphoreSlim(1, 1);
        
        // 设备状态
        // Device state
        private HardwareWalletDeviceInfo _deviceInfo;
        
        // 上次连接时间
        // Last connection time
        private DateTime _lastConnectionTime = DateTime.MinValue;
        
        // 连接超时检查定时器
        // Connection timeout check timer
        private System.Threading.Timer _connectionTimeoutTimer;
        
        // 是否需要初始化
        // Whether initialization is needed
        private bool _needInitialization = true;

        /// <summary>
        /// 获取硬件钱包类型
        /// Get hardware wallet type
        /// </summary>
        public HardwareWalletType WalletType => HardwareWalletType.Ledger;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public LedgerHardwareWallet(ILogger<LedgerHardwareWallet> logger)
        {
            _logger = logger;
            
            // 初始化HID设备
            // Initialize HID device
            WindowsHidDeviceFactory.Register(null, null);
            
            // 初始化超时检查定时器
            // Initialize timeout check timer
            InitializeConnectionTimeoutTimer();
        }
        
        /// <summary>
        /// 初始化连接超时检查定时器
        /// Initialize connection timeout check timer
        /// </summary>
        private void InitializeConnectionTimeoutTimer()
        {
            // 创建定时器，每30秒检查一次连接是否超时
            // Create timer to check if connection is timed out every 30 seconds
            _connectionTimeoutTimer = new System.Threading.Timer(CheckConnectionTimeout, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }
        
        /// <summary>
        /// 检查连接是否超时
        /// Check if connection is timed out
        /// </summary>
        private async void CheckConnectionTimeout(object state)
        {
            if (!_isConnected) return;
            
            // 如果连接已经超过5分钟没有活动，自动断开连接
            // If connection has been idle for more than 5 minutes, disconnect automatically
            if (DateTime.UtcNow - _lastConnectionTime > TimeSpan.FromMinutes(5))
            {
                _logger.LogInformation("检测到Ledger设备连接空闲超过5分钟，自动断开连接 / Detected Ledger device connection idle for more than 5 minutes, disconnecting automatically");
                await DisconnectAsync();
            }
        }

        /// <summary>
        /// 连接到Ledger设备
        /// Connect to Ledger device
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                await _deviceLock.WaitAsync();
                
                if (_isConnected && _ledgerManager != null)
                {
                    _logger.LogInformation("已经连接到Ledger设备 / Already connected to Ledger device");
                    _lastConnectionTime = DateTime.UtcNow;
                    return true;
                }
                
                _logger.LogInformation("正在连接到Ledger设备... / Connecting to Ledger device...");
                
                // 注册设备工厂
                // Register device factory
                WindowsHidDeviceFactory.Register(null, null);
                
                // 获取设备定义列表
                // Get device definition list
                var deviceDefinitions = new List<FilterDeviceDefinition>();
                
                // 添加Ledger设备定义
                // Add Ledger device definitions
                foreach (var vendorId in LEDGER_VENDOR_IDS)
                {
                    deviceDefinitions.Add(new FilterDeviceDefinition
                    {
                        VendorId = vendorId,
                        UsagePage = 0xFFA0
                    });
                }
                
                // 创建设备管理器
                // Create device manager
                var deviceManager = new DeviceManager(deviceDefinitions);
                
                // 初始化设备
                // Initialize devices
                await deviceManager.InitializeAsync();
                
                // 获取连接的设备
                // Get connected devices
                var devices = await deviceManager.GetDevicesAsync();
                
                if (!devices.Any())
                {
                    _logger.LogWarning("未找到Ledger设备，请确保设备已连接并处于正确的应用程序模式 / No Ledger device found, please make sure the device is connected and in the correct app mode");
                    return false;
                }
                
                // 获取第一个设备
                // Get first device
                var device = devices.First();
                
                // 连接设备
                // Connect device
                await device.InitializeAsync();
                
                // 创建Ledger管理器
                // Create Ledger manager
                _ledgerManager = new LedgerManager(device);
                
                // 获取当前状态
                // Get current state
                try
                {
                    _currentState = await _ledgerManager.GetApplicationStateAsync();
                    
                    if (_currentState == null)
                    {
                        _logger.LogWarning("无法获取Ledger设备状态，请确保设备已解锁并打开了以太坊或比特币应用 / Unable to get Ledger device state, please make sure the device is unlocked and the Ethereum or Bitcoin app is open");
                        await device.CloseAsync();
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "获取Ledger设备状态时出错 / Error getting Ledger device state");
                    await device.CloseAsync();
                    return false;
                }
                
                _isConnected = true;
                _lastConnectionTime = DateTime.UtcNow;
                
                // 初始化设备信息
                // Initialize device info
                await InitializeDeviceInfoAsync();
                
                _logger.LogInformation("已成功连接到Ledger设备 / Successfully connected to Ledger device");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "连接到Ledger设备时出错 / Error connecting to Ledger device");
                _isConnected = false;
                _ledgerManager = null;
                return false;
            }
            finally
            {
                _deviceLock.Release();
            }
        }
        
        /// <summary>
        /// 初始化设备信息
        /// Initialize device info
        /// </summary>
        private async Task InitializeDeviceInfoAsync()
        {
            try
            {
                _logger.LogInformation("正在获取Ledger设备信息... / Getting Ledger device information...");
                
                // 获取应用信息
                // Get application info
                var appInfo = await _ledgerManager.GetAppInfoAsync();
                
                // 创建设备信息
                // Create device info
                _deviceInfo = new HardwareWalletDeviceInfo
                {
                    Model = "Ledger Nano",
                    FirmwareVersion = appInfo.VersionString,
                    SerialNumber = "N/A", // Ledger不提供序列号 / Ledger does not provide serial number
                    IsUnlocked = true, // 如果可以获取应用信息，则设备已解锁 / If app info can be retrieved, device is unlocked
                    IsInitialized = true, // 如果可以获取应用信息，则设备已初始化 / If app info can be retrieved, device is initialized
                    SupportedFeatures = new List<string>
                    {
                        "Bitcoin",
                        "Ethereum",
                        "Transaction Signing",
                        "Address Verification"
                    }
                };
                
                _logger.LogInformation("成功获取Ledger设备信息 / Successfully got Ledger device information");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取Ledger设备信息时出错 / Error getting Ledger device information");
                throw;
            }
        }

        /// <summary>
        /// 断开与Ledger设备的连接
        /// Disconnect from Ledger device
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (!_isConnected || _ledgerManager == null)
            {
                return;
            }

            try
            {
                await _deviceLock.WaitAsync();
                
                _logger.LogInformation("正在断开与Ledger设备的连接... / Disconnecting from Ledger device...");
                
                try
                {
                    // 关闭设备
                    // Close device
                    if (_ledgerManager.DeviceConnectionHandle != null)
                    {
                        await _ledgerManager.DeviceConnectionHandle.CloseAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "关闭Ledger设备连接时出错，将继续断开连接过程 / Error closing Ledger device connection, will continue disconnection process");
                }
                
                _isConnected = false;
                _ledgerManager = null;
                _currentState = null;
                _deviceInfo = null;
                
                _logger.LogInformation("已成功断开与Ledger设备的连接 / Successfully disconnected from Ledger device");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "断开与Ledger设备的连接时出错 / Error disconnecting from Ledger device");
                // 即使出错，也设置为断开状态
                // Set to disconnected state even if error occurs
                _isConnected = false;
                _ledgerManager = null;
                _currentState = null;
                _deviceInfo = null;
            }
            finally
            {
                _deviceLock.Release();
            }
        }

        /// <summary>
        /// 检查Ledger设备是否已连接
        /// Check if Ledger device is connected
        /// </summary>
        public async Task<bool> IsConnectedAsync()
        {
            if (!_isConnected || _ledgerManager == null)
            {
                return false;
            }
            
            try
            {
                // 尝试获取当前状态以验证连接
                // Try to get current state to verify connection
                var state = await _ledgerManager.GetApplicationStateAsync();
                _currentState = state;
                _lastConnectionTime = DateTime.UtcNow;
                return state != null;
            }
            catch
            {
                _isConnected = false;
                return false;
            }
        }

        /// <summary>
        /// 获取Ledger设备信息
        /// Get Ledger device information
        /// </summary>
        public async Task<HardwareWalletDeviceInfo> GetDeviceInfoAsync()
        {
            EnsureConnected();

            try
            {
                // 如果已有设备信息，直接返回
                // If device info already exists, return it directly
                if (_deviceInfo != null)
                {
                    return _deviceInfo;
                }
                
                // 否则初始化设备信息
                // Otherwise initialize device info
                await InitializeDeviceInfoAsync();
                return _deviceInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取Ledger设备信息时出错 / Error getting Ledger device information");
                throw;
            }
        }
        
        /// <summary>
        /// 确保已连接
        /// Ensure connected
        /// </summary>
        private void EnsureConnected()
        {
            if (!_isConnected || _ledgerManager == null)
            {
                throw new InvalidOperationException("未连接到Ledger设备，请先连接 / Not connected to Ledger device, please connect first");
            }
            
            _lastConnectionTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 获取地址
        /// Get address
        /// </summary>
        public async Task<string> GetAddressAsync(string coinType, string derivationPath, bool displayOnDevice = false)
        {
            EnsureConnected();
            
            _logger.LogInformation($"正在获取{coinType}地址，派生路径：{derivationPath}，设备显示：{displayOnDevice} / Getting {coinType} address, derivation path: {derivationPath}, display on device: {displayOnDevice}");
            
            try
            {
                // 根据币种类型选择不同的方法
                // Choose different methods based on coin type
                switch (coinType.ToLowerInvariant())
                {
                    case "btc":
                    case "bitcoin":
                        return await GetBitcoinAddressAsync(derivationPath, displayOnDevice);
                    
                    case "eth":
                    case "ethereum":
                        return await GetEthereumAddressAsync(derivationPath, displayOnDevice);
                    
                    default:
                        throw new NotSupportedException($"不支持的币种类型：{coinType} / Unsupported coin type: {coinType}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取{coinType}地址时出错 / Error getting {coinType} address");
                throw;
            }
        }
        
        /// <summary>
        /// 获取比特币地址
        /// Get Bitcoin address
        /// </summary>
        private async Task<string> GetBitcoinAddressAsync(string derivationPath, bool displayOnDevice)
        {
            // 确认已打开比特币应用
            // Confirm Bitcoin app is open
            if (_currentState == null || !_currentState.IsConnected || _currentState.ApplicationName != "Bitcoin")
            {
                _logger.LogWarning("请在Ledger设备上打开比特币应用 / Please open Bitcoin app on Ledger device");
                throw new InvalidOperationException("未打开比特币应用 / Bitcoin app not open");
            }
            
            try
            {
                // 生成BIP32路径
                // Generate BIP32 path
                var bip32Path = GetBip32Path(derivationPath);
                
                // 使用Ledger管理器获取公钥
                // Use Ledger manager to get public key
                var response = await _ledgerManager.GetPublicKeyAsync(bip32Path, displayOnDevice);
                
                if (response == null)
                {
                    throw new InvalidOperationException("无法获取公钥 / Unable to get public key");
                }
                
                // 使用公钥生成比特币地址
                // Generate Bitcoin address from public key
                var address = BitcoinAddressHelper.GetBitcoinAddress(response.PublicKey, true);
                
                _logger.LogInformation($"成功获取比特币地址：{address} / Successfully got Bitcoin address: {address}");
                return address;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取比特币地址时出错 / Error getting Bitcoin address");
                throw;
            }
        }
        
        /// <summary>
        /// 获取以太坊地址
        /// Get Ethereum address
        /// </summary>
        private async Task<string> GetEthereumAddressAsync(string derivationPath, bool displayOnDevice)
        {
            // 确认已打开以太坊应用
            // Confirm Ethereum app is open
            if (_currentState == null || !_currentState.IsConnected || _currentState.ApplicationName != "Ethereum")
            {
                _logger.LogWarning("请在Ledger设备上打开以太坊应用 / Please open Ethereum app on Ledger device");
                throw new InvalidOperationException("未打开以太坊应用 / Ethereum app not open");
            }
            
            try
            {
                // 生成BIP32路径
                // Generate BIP32 path
                var bip32Path = GetBip32Path(derivationPath);
                
                // 使用Ledger管理器获取公钥
                // Use Ledger manager to get public key
                var response = await _ledgerManager.GetPublicKeyAsync(bip32Path, displayOnDevice);
                
                if (response == null)
                {
                    throw new InvalidOperationException("无法获取公钥 / Unable to get public key");
                }
                
                // 使用公钥生成以太坊地址
                // Generate Ethereum address from public key
                var address = EthereumAddressHelper.GetEthereumAddress(response.PublicKey);
                
                _logger.LogInformation($"成功获取以太坊地址：{address} / Successfully got Ethereum address: {address}");
                return address;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取以太坊地址时出错 / Error getting Ethereum address");
                throw;
            }
        }
        
        /// <summary>
        /// 将派生路径转换为BIP32路径
        /// Convert derivation path to BIP32 path
        /// </summary>
        private static uint[] GetBip32Path(string derivationPath)
        {
            // 移除前缀"m/"
            // Remove "m/" prefix
            if (derivationPath.StartsWith("m/"))
            {
                derivationPath = derivationPath.Substring(2);
            }
            
            // 分割路径组件
            // Split path components
            var pathComponents = derivationPath.Split('/');
            var path = new uint[pathComponents.Length];
            
            for (int i = 0; i < pathComponents.Length; i++)
            {
                string component = pathComponents[i];
                
                // 检查是否有硬化标记（'）
                // Check if there's a hardening marker (')
                bool isHardened = component.EndsWith("'") || component.EndsWith("h") || component.EndsWith("H");
                
                if (isHardened)
                {
                    // 移除硬化标记
                    // Remove hardening marker
                    component = component.Substring(0, component.Length - 1);
                }
                
                // 解析组件值
                // Parse component value
                if (!uint.TryParse(component, out uint value))
                {
                    throw new FormatException($"无效的派生路径组件：{pathComponents[i]} / Invalid derivation path component: {pathComponents[i]}");
                }
                
                // 如果是硬化路径，添加硬化标记 (0x80000000)
                // If hardened path, add hardening marker (0x80000000)
                if (isHardened)
                {
                    value |= 0x80000000;
                }
                
                path[i] = value;
            }
            
            return path;
        }
        
        /// <summary>
        /// 签名交易
        /// Sign transaction
        /// </summary>
        public async Task<string> SignTransactionAsync(string coinType, string derivationPath, string unsignedTransaction)
        {
            EnsureConnected();
            
            _logger.LogInformation($"正在签名{coinType}交易，派生路径：{derivationPath} / Signing {coinType} transaction, derivation path: {derivationPath}");
            
            try
            {
                // 根据币种类型选择不同的方法
                // Choose different methods based on coin type
                switch (coinType.ToLowerInvariant())
                {
                    case "btc":
                    case "bitcoin":
                        return await SignBitcoinTransactionAsync(derivationPath, unsignedTransaction);
                    
                    case "eth":
                    case "ethereum":
                        return await SignEthereumTransactionAsync(derivationPath, unsignedTransaction);
                    
                    default:
                        throw new NotSupportedException($"不支持的币种类型：{coinType} / Unsupported coin type: {coinType}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"签名{coinType}交易时出错 / Error signing {coinType} transaction");
                throw;
            }
        }
        
        /// <summary>
        /// 签名比特币交易
        /// Sign Bitcoin transaction
        /// </summary>
        private async Task<string> SignBitcoinTransactionAsync(string derivationPath, string unsignedTransaction)
        {
            // 确认已打开比特币应用
            // Confirm Bitcoin app is open
            if (_currentState == null || !_currentState.IsConnected || _currentState.ApplicationName != "Bitcoin")
            {
                _logger.LogWarning("请在Ledger设备上打开比特币应用 / Please open Bitcoin app on Ledger device");
                throw new InvalidOperationException("未打开比特币应用 / Bitcoin app not open");
            }
            
            try
            {
                // 解析未签名的交易
                // Parse unsigned transaction
                byte[] transactionBytes = Convert.FromHexString(unsignedTransaction);
                
                // 生成BIP32路径
                // Generate BIP32 path
                var bip32Path = GetBip32Path(derivationPath);
                
                // TODO: 实际实现中需要根据比特币交易结构进行处理
                // TODO: In actual implementation, need to process according to Bitcoin transaction structure
                // 由于比特币交易签名复杂，这里仅提供基本框架
                // Since Bitcoin transaction signing is complex, only providing basic framework here
                
                _logger.LogInformation("正在请求用户确认交易 / Requesting user to confirm transaction");
                
                // 向Ledger设备发送签名请求
                // Send signing request to Ledger device
                var signatureResponse = await _ledgerManager.SignTransactionAsync(transactionBytes, bip32Path);
                
                if (signatureResponse == null || signatureResponse.Signature == null)
                {
                    throw new InvalidOperationException("签名响应为空 / Signature response is null");
                }
                
                // 将签名应用到交易中
                // Apply signature to transaction
                string signedTransaction = BitcoinTransactionHelper.ApplySignatureToTransaction(unsignedTransaction, Convert.ToHexString(signatureResponse.Signature).ToLowerInvariant());
                
                _logger.LogInformation("交易签名成功 / Transaction signed successfully");
                return signedTransaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "签名比特币交易时出错 / Error signing Bitcoin transaction");
                throw;
            }
        }
        
        /// <summary>
        /// 签名以太坊交易
        /// Sign Ethereum transaction
        /// </summary>
        private async Task<string> SignEthereumTransactionAsync(string derivationPath, string unsignedTransaction)
        {
            // 确认已打开以太坊应用
            // Confirm Ethereum app is open
            if (_currentState == null || !_currentState.IsConnected || _currentState.ApplicationName != "Ethereum")
            {
                _logger.LogWarning("请在Ledger设备上打开以太坊应用 / Please open Ethereum app on Ledger device");
                throw new InvalidOperationException("未打开以太坊应用 / Ethereum app not open");
            }
            
            try
            {
                // 解析未签名的交易
                // Parse unsigned transaction
                // 这里假设unsignedTransaction包含RLP编码的以太坊交易
                // Here we assume unsignedTransaction contains RLP encoded Ethereum transaction
                byte[] transactionBytes = Convert.FromHexString(unsignedTransaction);
                
                // 生成BIP32路径
                // Generate BIP32 path
                var bip32Path = GetBip32Path(derivationPath);
                
                _logger.LogInformation("正在请求用户确认交易 / Requesting user to confirm transaction");
                
                // 向Ledger设备发送签名请求
                // Send signing request to Ledger device
                var signatureResponse = await _ledgerManager.SignTransactionAsync(transactionBytes, bip32Path);
                
                if (signatureResponse == null || signatureResponse.Signature == null)
                {
                    throw new InvalidOperationException("签名响应为空 / Signature response is null");
                }
                
                // 将签名应用到交易中
                // Apply signature to transaction
                string signedTransaction = EthereumTransactionHelper.ApplySignatureToTransaction(unsignedTransaction, Convert.ToHexString(signatureResponse.Signature).ToLowerInvariant());
                
                _logger.LogInformation("交易签名成功 / Transaction signed successfully");
                return signedTransaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "签名以太坊交易时出错 / Error signing Ethereum transaction");
                throw;
            }
        }

        /// <summary>
        /// 释放资源
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            // 如果已连接，断开连接
            // If connected, disconnect
            if (_isConnected)
            {
                DisconnectAsync().Wait();
            }
            
            // 停止并释放定时器
            // Stop and dispose timer
            _connectionTimeoutTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _connectionTimeoutTimer?.Dispose();
            _connectionTimeoutTimer = null;
            
            // 释放设备锁
            // Dispose device lock
            _deviceLock?.Dispose();
        }
    }
    
    /// <summary>
    /// 比特币地址助手
    /// Bitcoin address helper
    /// </summary>
    internal static class BitcoinAddressHelper
    {
        /// <summary>
        /// 获取比特币地址
        /// Get Bitcoin address
        /// </summary>
        public static string GetBitcoinAddress(byte[] publicKey, bool compressed)
        {
            // 简化实现，实际应用中应使用完整的比特币库
            // Simplified implementation, should use complete Bitcoin library in actual application
            return "bc1" + Convert.ToHexString(publicKey.Take(20).ToArray()).ToLowerInvariant();
        }
    }
    
    /// <summary>
    /// 以太坊地址助手
    /// Ethereum address helper
    /// </summary>
    internal static class EthereumAddressHelper
    {
        /// <summary>
        /// 获取以太坊地址
        /// Get Ethereum address
        /// </summary>
        public static string GetEthereumAddress(byte[] publicKey)
        {
            // 简化实现，实际应用中应使用完整的以太坊库计算Keccak-256哈希等
            // Simplified implementation, should use complete Ethereum library to calculate Keccak-256 hash, etc.
            return "0x" + Convert.ToHexString(publicKey.Skip(1).Take(20).ToArray()).ToLowerInvariant();
        }
    }
    
    /// <summary>
    /// 比特币交易助手
    /// Bitcoin transaction helper
    /// </summary>
    internal static class BitcoinTransactionHelper
    {
        /// <summary>
        /// 将签名应用到交易中
        /// Apply signature to transaction
        /// </summary>
        public static string ApplySignatureToTransaction(string unsignedTransaction, string signature)
        {
            // 简化实现，实际应用中应使用完整的比特币库
            // Simplified implementation, should use complete Bitcoin library in actual application
            return unsignedTransaction.Substring(0, unsignedTransaction.Length - 2) + signature + "01";
        }
    }
    
    /// <summary>
    /// 以太坊交易助手
    /// Ethereum transaction helper
    /// </summary>
    internal static class EthereumTransactionHelper
    {
        /// <summary>
        /// 将签名应用到交易中
        /// Apply signature to transaction
        /// </summary>
        public static string ApplySignatureToTransaction(string unsignedTransaction, string signature)
        {
            // 简化实现，实际应用中应使用完整的以太坊库
            // Simplified implementation, should use complete Ethereum library in actual application
            return "0x" + unsignedTransaction.Substring(2) + signature;
        }
    }
} 