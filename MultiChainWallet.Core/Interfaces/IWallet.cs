using System;
using System.Threading.Tasks;

namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// 钱包接口定义
    /// Wallet interface definition
    /// </summary>
    public interface IWallet
    {
        /// <summary>
        /// 创建新钱包
        /// Create a new wallet
        /// </summary>
        /// <returns>钱包地址 / Wallet address</returns>
        Task<string> CreateWalletAsync();

        /// <summary>
        /// 获取钱包余额
        /// Get wallet balance
        /// </summary>
        /// <param name="address">钱包地址 / Wallet address</param>
        /// <returns>余额 / Balance</returns>
        Task<decimal> GetBalanceAsync(string address);

        /// <summary>
        /// 发送交易
        /// Send transaction
        /// </summary>
        /// <param name="fromAddress">发送地址 / From address</param>
        /// <param name="toAddress">接收地址 / To address</param>
        /// <param name="amount">金额 / Amount</param>
        /// <returns>交易哈希 / Transaction hash</returns>
        Task<string> SendTransactionAsync(string fromAddress, string toAddress, decimal amount);

        /// <summary>
        /// 获取当前钱包地址
        /// Get current wallet address
        /// </summary>
        Task<string> GetCurrentAddress();

        /// <summary>
        /// 解锁钱包
        /// Unlock wallet
        /// </summary>
        Task<bool> UnlockWallet(string password);

        /// <summary>
        /// 锁定钱包
        /// Lock wallet
        /// </summary>
        Task<bool> LockWallet();

        /// <summary>
        /// 验证密码
        /// Validate password
        /// </summary>
        Task<bool> ValidatePassword(string password);
    }
}
