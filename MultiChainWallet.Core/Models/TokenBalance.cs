namespace MultiChainWallet.Core.Models
{
    /// <summary>
    /// 代币余额实体
    /// Token balance entity
    /// </summary>
    public class TokenBalance
    {
        /// <summary>
        /// 钱包地址
        /// Wallet address
        /// </summary>
        public string WalletAddress { get; set; }

        /// <summary>
        /// 代币合约地址
        /// Token contract address
        /// </summary>
        public string TokenContractAddress { get; set; }

        /// <summary>
        /// 代币名称
        /// Token name
        /// </summary>
        public string TokenName { get; set; }

        /// <summary>
        /// 代币符号
        /// Token symbol
        /// </summary>
        public string TokenSymbol { get; set; }

        /// <summary>
        /// 代币精度
        /// Token decimals
        /// </summary>
        public int Decimals { get; set; }

        /// <summary>
        /// 余额
        /// Balance
        /// </summary>
        public decimal Balance { get; set; }

        /// <summary>
        /// 代币价格（USD）
        /// Token price (USD)
        /// </summary>
        public decimal? UsdPrice { get; set; }

        /// <summary>
        /// 是否启用
        /// Is enabled
        /// </summary>
        public bool IsEnabled { get; set; }
    }
}
