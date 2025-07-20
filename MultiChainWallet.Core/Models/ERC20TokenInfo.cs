using System;

namespace MultiChainWallet.Core.Models
{
    /// <summary>
    /// ERC20代币信息模型
    /// ERC20 token information model
    /// </summary>
    public class ERC20TokenInfo
    {
        public string ContractAddress { get; set; }
        public string Symbol { get; set; }
        public string Name { get; set; }
        public int Decimals { get; set; }
        public decimal Balance { get; set; }
        public decimal USDPrice { get; set; }
        public decimal Value => Balance * USDPrice;
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// ERC20代币交易历史记录
    /// ERC20 token transaction history record
    /// </summary>
    public class ERC20TransactionHistory
    {
        public string TransactionHash { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public decimal Amount { get; set; }
        public string TokenAddress { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsConfirmed { get; set; }
        public decimal GasUsed { get; set; }
        public decimal GasPrice { get; set; }
    }
}
