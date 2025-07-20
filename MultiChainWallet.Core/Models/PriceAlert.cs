using System;

namespace MultiChainWallet.Core.Models
{
    /// <summary>
    /// 价格提醒
    /// Price alert
    /// </summary>
    public class PriceAlert
    {
        /// <summary>
        /// 提醒ID
        /// Alert ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 币种符号
        /// Currency symbol
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// 法币符号
        /// Fiat currency symbol
        /// </summary>
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// 提醒类型
        /// Alert type
        /// </summary>
        public PriceAlertType AlertType { get; set; }

        /// <summary>
        /// 价格阈值
        /// Price threshold
        /// </summary>
        public decimal Threshold { get; set; }

        /// <summary>
        /// 是否激活
        /// Whether alert is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// Creation time
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 最后触发时间
        /// Last triggered time
        /// </summary>
        public DateTime? LastTriggered { get; set; }

        /// <summary>
        /// 触发次数
        /// Number of times triggered
        /// </summary>
        public int TriggerCount { get; set; }

        /// <summary>
        /// 创建时的价格
        /// Price at creation
        /// </summary>
        public decimal PriceAtCreation { get; set; }
    }
} 