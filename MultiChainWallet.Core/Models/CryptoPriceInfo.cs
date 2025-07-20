using System;

namespace MultiChainWallet.Core.Models
{
    /// <summary>
    /// 加密货币价格信息模型
    /// Cryptocurrency price information model
    /// </summary>
    public class CryptoPriceInfo
    {
        /// <summary>
        /// 币种符号（例如BTC, ETH）
        /// Coin symbol (e.g. BTC, ETH)
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// 计价货币（例如USD, EUR）
        /// Currency (e.g. USD, EUR)
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// 当前价格
        /// Current price
        /// </summary>
        public decimal CurrentPrice { get; set; }

        /// <summary>
        /// 24小时价格变化（绝对值）
        /// 24h price change (absolute value)
        /// </summary>
        public decimal PriceChange24h { get; set; }

        /// <summary>
        /// 24小时价格变化百分比
        /// 24h price change percentage
        /// </summary>
        public decimal PriceChangePercentage24h { get; set; }

        /// <summary>
        /// 24小时最高价
        /// 24h high
        /// </summary>
        public decimal High24h { get; set; }

        /// <summary>
        /// 24小时最低价
        /// 24h low
        /// </summary>
        public decimal Low24h { get; set; }

        /// <summary>
        /// 上次更新时间
        /// Last update time
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// 是否来自缓存
        /// Whether from cache
        /// </summary>
        public bool IsFromCache { get; set; }
    }
} 