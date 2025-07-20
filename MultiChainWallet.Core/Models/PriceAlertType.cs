using System;

namespace MultiChainWallet.Core.Models
{
    /// <summary>
    /// 价格提醒类型
    /// Price alert type
    /// </summary>
    public enum PriceAlertType
    {
        /// <summary>
        /// 价格上涨至阈值
        /// Price rises to threshold
        /// </summary>
        PriceAbove,

        /// <summary>
        /// 价格下跌至阈值
        /// Price falls to threshold
        /// </summary>
        PriceBelow,

        /// <summary>
        /// 价格变化百分比超过阈值
        /// Price change percentage exceeds threshold
        /// </summary>
        PercentageChange
    }
} 