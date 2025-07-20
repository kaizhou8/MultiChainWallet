using System;

namespace MultiChainWallet.Core.Models
{
    /// <summary>
    /// 交易信息模型
    /// Transaction information model
    /// </summary>
    public class TransactionInfo
    {
        /// <summary>
        /// 交易哈希
        /// Transaction hash
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// 发送地址
        /// From address
        /// </summary>
        public string FromAddress { get; set; }

        /// <summary>
        /// 接收地址
        /// To address
        /// </summary>
        public string ToAddress { get; set; }

        /// <summary>
        /// 金额
        /// Amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 交易状态
        /// Transaction status
        /// </summary>
        public TransactionStatus Status { get; set; }

        /// <summary>
        /// 交易时间戳
        /// Transaction timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
