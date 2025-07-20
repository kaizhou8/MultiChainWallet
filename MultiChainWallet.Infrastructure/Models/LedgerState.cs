using System;
using System.Collections.Generic;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// Ledger设备状态信息
    /// Ledger device state information
    /// </summary>
    public class LedgerState
    {
        /// <summary>
        /// 设备型号
        /// Device model
        /// </summary>
        public string? Model { get; set; }
        
        /// <summary>
        /// 固件版本
        /// Firmware version
        /// </summary>
        public string? FirmwareVersion { get; set; }
        
        /// <summary>
        /// 设备序列号
        /// Device serial number
        /// </summary>
        public string? SerialNumber { get; set; }
        
        /// <summary>
        /// 是否已解锁
        /// Whether the device is unlocked
        /// </summary>
        public bool IsUnlocked { get; set; }
        
        /// <summary>
        /// 当前打开的应用
        /// Currently open application
        /// </summary>
        public string? CurrentApp { get; set; }
        
        /// <summary>
        /// 支持的应用
        /// Supported applications
        /// </summary>
        public List<string>? SupportedApps { get; set; }
        
        /// <summary>
        /// 是否处于应用模式
        /// Whether the device is in application mode
        /// </summary>
        public bool IsInAppMode { get; set; }
        
        /// <summary>
        /// 是否已初始化
        /// Whether the device is initialized
        /// </summary>
        public bool IsInitialized { get; set; }
    }
} 