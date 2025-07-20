using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MultiChainWallet.Core.Models;

namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// 硬件钱包接口
    /// Hardware wallet interface
    /// </summary>
    public interface IHardwareWallet
    {
        /// <summary>
        /// 钱包类型
        /// Wallet type
        /// </summary>
        HardwareWalletType WalletType { get; }

        /// <summary>
        /// 连接到硬件钱包
        /// Connect to hardware wallet
        /// </summary>
        Task<bool> ConnectAsync();

        /// <summary>
        /// 断开硬件钱包连接
        /// Disconnect from hardware wallet
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// 检查是否已连接
        /// Check if connected
        /// </summary>
        Task<bool> IsConnectedAsync();

        /// <summary>
        /// 获取设备信息
        /// Get device information
        /// </summary>
        Task<HardwareWalletDeviceInfo> GetDeviceInfoAsync();

        /// <summary>
        /// 获取地址
        /// Get address
        /// </summary>
        /// <param name="coinType">币种类型（如"btc"或"eth"）/ Coin type (like "btc" or "eth")</param>
        /// <param name="derivationPath">派生路径（如"m/44'/0'/0'/0/0"）/ Derivation path (like "m/44'/0'/0'/0/0")</param>
        /// <param name="displayOnDevice">是否在设备上显示地址 / Whether to display address on device</param>
        Task<string> GetAddressAsync(string coinType, string derivationPath, bool displayOnDevice = false);

        /// <summary>
        /// 签名交易
        /// Sign transaction
        /// </summary>
        /// <param name="coinType">币种类型（如"btc"或"eth"）/ Coin type (like "btc" or "eth")</param>
        /// <param name="derivationPath">派生路径（如"m/44'/0'/0'/0/0"）/ Derivation path (like "m/44'/0'/0'/0/0")</param>
        /// <param name="unsignedTransaction">未签名的交易（十六进制字符串）/ Unsigned transaction (hex string)</param>
        /// <returns>已签名的交易（十六进制字符串）/ Signed transaction (hex string)</returns>
        Task<string> SignTransactionAsync(string coinType, string derivationPath, string unsignedTransaction);
    }
} 