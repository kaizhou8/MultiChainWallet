using System;

namespace MultiChainWallet.Core.Models
{
    /// <summary>
    /// 加密货币市场信息模型
    /// Cryptocurrency market information model
    /// </summary>
    public class CryptoMarketInfo
    {
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
        /// 市值
        /// Market cap
        /// </summary>
        public decimal MarketCap { get; set; }

        /// <summary>
        /// 市值排名
        /// Market cap rank
        /// </summary>
        public int MarketCapRank { get; set; }

        /// <summary>
        /// 完全稀释估值
        /// Fully diluted valuation
        /// </summary>
        public decimal FullyDilutedValuation { get; set; }

        /// <summary>
        /// 总供应量
        /// Total supply
        /// </summary>
        public decimal TotalSupply { get; set; }

        /// <summary>
        /// 最大供应量
        /// Max supply
        /// </summary>
        public decimal? MaxSupply { get; set; }

        /// <summary>
        /// 流通供应量
        /// Circulating supply
        /// </summary>
        public decimal CirculatingSupply { get; set; }

        /// <summary>
        /// 24小时交易量
        /// 24h volume
        /// </summary>
        public decimal Volume24h { get; set; }

        /// <summary>
        /// 价格信息
        /// Price information
        /// </summary>
        public CryptoPriceInfo PriceInfo { get; set; }

        /// <summary>
        /// 上次更新时间
        /// Last update time
        /// </summary>
        public DateTime LastUpdated { get; set; }
    }
} 