namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// 交易确认服务接口
    /// Transaction confirmation service interface
    /// </summary>
    public interface ITransactionConfirmationService
    {
        /// <summary>
        /// 请求交易确认
        /// Request transaction confirmation
        /// </summary>
        /// <param name="fromAddress">发送方地址 / From address</param>
        /// <param name="toAddress">接收方地址 / To address</param>
        /// <param name="amount">金额 / Amount</param>
        /// <param name="currency">币种 / Currency</param>
        /// <param name="fee">手续费 / Fee</param>
        /// <returns>如果交易被确认则返回true / Returns true if transaction is confirmed</returns>
        Task<bool> RequestConfirmationAsync(string fromAddress, string toAddress, string amount, string currency, string fee);

        /// <summary>
        /// 检查是否需要交易确认
        /// Check if transaction confirmation is required
        /// </summary>
        Task<bool> IsConfirmationRequiredAsync();

        /// <summary>
        /// 获取可用的认证方式
        /// Get available authentication methods
        /// </summary>
        Task<(bool IsBiometricAvailable, bool IsTotpEnabled)> GetAvailableAuthMethodsAsync();
    }
} 