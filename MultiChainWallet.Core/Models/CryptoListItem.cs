namespace MultiChainWallet.Core.Models
{
    /// <summary>
    /// 加密货币列表项模型，用于趋势币种等
    /// Cryptocurrency list item model, used for trending coins etc.
    /// </summary>
    public class CryptoListItem
    {
        /// <summary>
        /// 币种ID
        /// Coin ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 币种符号（例如BTC, ETH）
        /// Coin symbol (e.g. BTC, ETH)
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// 币种名称
        /// Coin name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 图标URL
        /// Icon URL
        /// </summary>
        public string IconUrl { get; set; }

        /// <summary>
        /// 当前价格
        /// Current price
        /// </summary>
        public decimal CurrentPrice { get; set; }

        /// <summary>
        /// 市值排名
        /// Market cap rank
        /// </summary>
        public int MarketCapRank { get; set; }

        /// <summary>
        /// 24小时价格变化百分比
        /// 24h price change percentage
        /// </summary>
        public decimal PriceChangePercentage24h { get; set; }
    }
} 