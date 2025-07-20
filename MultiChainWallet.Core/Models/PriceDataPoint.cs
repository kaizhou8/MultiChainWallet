using System;

namespace MultiChainWallet.Core.Models
{
    /// <summary>
    /// 价格数据点模型，用于历史价格数据
    /// Price data point model, used for historical price data
    /// </summary>
    public class PriceDataPoint
    {
        /// <summary>
        /// 时间戳
        /// Timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 价格
        /// Price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 市值
        /// Market cap
        /// </summary>
        public decimal MarketCap { get; set; }

        /// <summary>
        /// 交易量
        /// Volume
        /// </summary>
        public decimal Volume { get; set; }
    }
} 