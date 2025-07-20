using System;
using System.Collections.Generic;
using Microsoft.Maui.Graphics;

namespace MultiChainWallet.Core.Models
{
    /// <summary>
    /// 硬件钱包类型
    /// Hardware wallet type
    /// </summary>
    public enum HardwareWalletType
    {
        /// <summary>
        /// Ledger钱包
        /// Ledger wallet
        /// </summary>
        Ledger,
        
        /// <summary>
        /// Trezor钱包
        /// Trezor wallet
        /// </summary>
        Trezor,
        
        /// <summary>
        /// KeepKey钱包
        /// KeepKey wallet
        /// </summary>
        KeepKey
    }

    /// <summary>
    /// 币种类型
    /// Coin type
    /// </summary>
    public enum CoinType
    {
        /// <summary>
        /// 比特币
        /// Bitcoin
        /// </summary>
        Bitcoin,
        
        /// <summary>
        /// 以太坊
        /// Ethereum
        /// </summary>
        Ethereum
    }

    /// <summary>
    /// 硬件钱包设备信息
    /// Hardware wallet device information
    /// </summary>
    public class HardwareWalletDeviceInfo
    {
        /// <summary>
        /// 设备型号
        /// Device model
        /// </summary>
        public string Model { get; set; }
        
        /// <summary>
        /// 固件版本
        /// Firmware version
        /// </summary>
        public string FirmwareVersion { get; set; }
        
        /// <summary>
        /// 序列号
        /// Serial number
        /// </summary>
        public string SerialNumber { get; set; }
        
        /// <summary>
        /// 是否已解锁
        /// Whether unlocked
        /// </summary>
        public bool IsUnlocked { get; set; }
        
        /// <summary>
        /// 是否已初始化
        /// Whether initialized
        /// </summary>
        public bool IsInitialized { get; set; }
        
        /// <summary>
        /// 支持的功能
        /// Supported features
        /// </summary>
        public List<string> SupportedFeatures { get; set; } = new List<string>();
    }

    /// <summary>
    /// 以太坊交易
    /// Ethereum transaction
    /// </summary>
    public class EthereumTransaction
    {
        /// <summary>
        /// 发送方地址
        /// From address
        /// </summary>
        public string From { get; set; }
        
        /// <summary>
        /// 接收方地址
        /// To address
        /// </summary>
        public string To { get; set; }
        
        /// <summary>
        /// 金额（以Wei为单位）
        /// Amount (in Wei)
        /// </summary>
        public string Value { get; set; }
        
        /// <summary>
        /// Gas价格（以Wei为单位）
        /// Gas price (in Wei)
        /// </summary>
        public string GasPrice { get; set; }
        
        /// <summary>
        /// Gas限制
        /// Gas limit
        /// </summary>
        public string GasLimit { get; set; }
        
        /// <summary>
        /// 数据
        /// Data
        /// </summary>
        public string Data { get; set; }
        
        /// <summary>
        /// 链ID
        /// Chain ID
        /// </summary>
        public int ChainId { get; set; }
        
        /// <summary>
        /// Nonce
        /// </summary>
        public string Nonce { get; set; }
    }

    /// <summary>
    /// 比特币交易
    /// Bitcoin transaction
    /// </summary>
    public class BitcoinTransaction
    {
        /// <summary>
        /// 交易输入
        /// Transaction inputs
        /// </summary>
        public List<BitcoinTransactionInput> Inputs { get; set; } = new List<BitcoinTransactionInput>();
        
        /// <summary>
        /// 交易输出
        /// Transaction outputs
        /// </summary>
        public List<BitcoinTransactionOutput> Outputs { get; set; } = new List<BitcoinTransactionOutput>();
        
        /// <summary>
        /// 版本
        /// Version
        /// </summary>
        public uint Version { get; set; } = 1;
        
        /// <summary>
        /// 锁定时间
        /// Lock time
        /// </summary>
        public uint LockTime { get; set; } = 0;
    }

    /// <summary>
    /// 比特币交易输入
    /// Bitcoin transaction input
    /// </summary>
    public class BitcoinTransactionInput
    {
        /// <summary>
        /// 前一个交易的哈希
        /// Previous transaction hash
        /// </summary>
        public string PreviousTxHash { get; set; }
        
        /// <summary>
        /// 前一个交易的输出索引
        /// Previous transaction output index
        /// </summary>
        public uint PreviousTxOutputIndex { get; set; }
        
        /// <summary>
        /// 脚本
        /// Script
        /// </summary>
        public string Script { get; set; }
        
        /// <summary>
        /// 序列号
        /// Sequence
        /// </summary>
        public uint Sequence { get; set; } = 0xFFFFFFFF;
        
        /// <summary>
        /// 金额（以Satoshi为单位）
        /// Amount (in Satoshi)
        /// </summary>
        public long Amount { get; set; }
    }

    /// <summary>
    /// 比特币交易输出
    /// Bitcoin transaction output
    /// </summary>
    public class BitcoinTransactionOutput
    {
        /// <summary>
        /// 地址
        /// Address
        /// </summary>
        public string Address { get; set; }
        
        /// <summary>
        /// 金额（以Satoshi为单位）
        /// Amount (in Satoshi)
        /// </summary>
        public long Amount { get; set; }
    }

    /// <summary>
    /// 硬件钱包列表项
    /// Hardware wallet list item
    /// </summary>
    public class HardwareWalletListItem : Microsoft.Maui.Controls.BindableObject
    {
        private bool _isDetected;

        /// <summary>
        /// 是否已检测到设备
        /// Whether the device is detected
        /// </summary>
        public bool IsDetected
        { 
            get => _isDetected;
            set
            {
                _isDetected = value;
                OnPropertyChanged(nameof(IsDetected));
                OnPropertyChanged(nameof(DetectionStatusText));
                OnPropertyChanged(nameof(DetectionStatusColor));
            }
        }

        /// <summary>
        /// 钱包类型
        /// Wallet type
        /// </summary>
        public HardwareWalletType WalletType { get; set; }

        /// <summary>
        /// 显示名称
        /// Display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 图像源
        /// Image source
        /// </summary>
        public string ImageSource { get; set; }

        /// <summary>
        /// 检测状态文本
        /// Detection status text
        /// </summary>
        public string DetectionStatusText => IsDetected ? "已检测到 / Detected" : "未检测到 / Not Detected";

        /// <summary>
        /// 检测状态颜色
        /// Detection status color
        /// </summary>
        public Color DetectionStatusColor => IsDetected ? Colors.Green : Colors.Red;
    }
} 