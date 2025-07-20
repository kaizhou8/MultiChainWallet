using System;
using System.Collections.Generic;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// Trezor设备特性信息
    /// Trezor device feature information
    /// </summary>
    public class TrezorFeatures
    {
        /// <summary>
        /// 供应商名称
        /// Vendor name
        /// </summary>
        public string? Vendor { get; set; }
        
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
        /// 是否已初始化
        /// Whether the device is initialized
        /// </summary>
        public bool IsInitialized { get; set; }
        
        /// <summary>
        /// 是否有PIN码保护
        /// Whether the device has PIN protection
        /// </summary>
        public bool HasPinProtection { get; set; }
        
        /// <summary>
        /// 是否已解锁
        /// Whether the device is unlocked
        /// </summary>
        public bool IsUnlocked { get; set; }
        
        /// <summary>
        /// 支持的币种
        /// Supported coins
        /// </summary>
        public List<string>? SupportedCoins { get; set; }
    }
} 