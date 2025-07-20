namespace MultiChainWallet.Core.Models
{
    /// <summary>
    /// 加密货币搜索结果模型
    /// Cryptocurrency search result model
    /// </summary>
    public class CryptoSearchResult
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
        /// 市值排名
        /// Market cap rank
        /// </summary>
        public int? MarketCapRank { get; set; }
    }
} 