using System;

namespace MultiChainWallet.Core.Models
{
    /// <summary>
    /// 交易记录实体
    /// Transaction record entity
    /// </summary>
    public class Transaction
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
        /// 代币合约地址（如果是代币交易）
        /// Token contract address (if token transaction)
        /// </summary>
        public string TokenContractAddress { get; set; }

        /// <summary>
        /// 交易时间
        /// Transaction time
        /// </summary>
        public DateTime TransactionTime { get; set; }

        /// <summary>
        /// 交易状态
        /// Transaction status
        /// </summary>
        public TransactionStatus Status { get; set; }

        /// <summary>
        /// 链类型
        /// Chain type
        /// </summary>
        public ChainType ChainType { get; set; }

        /// <summary>
        /// Gas价格（以太坊）
        /// Gas price (Ethereum)
        /// </summary>
        public decimal? GasPrice { get; set; }

        /// <summary>
        /// Gas限制（以太坊）
        /// Gas limit (Ethereum)
        /// </summary>
        public long? GasLimit { get; set; }

        /// <summary>
        /// 实际使用的Gas（以太坊）
        /// Gas used (Ethereum)
        /// </summary>
        public long? GasUsed { get; set; }

        /// <summary>
        /// 区块高度
        /// Block height
        /// </summary>
        public long? BlockHeight { get; set; }

        /// <summary>
        /// 确认数
        /// Number of confirmations
        /// </summary>
        public int Confirmations { get; set; }

        /// <summary>
        /// 备注
        /// Note
        /// </summary>
        public string Note { get; set; }
    }

    /// <summary>
    /// 交易状态枚举
    /// Transaction status enumeration
    /// </summary>
    public enum TransactionStatus
    {
        /// <summary>
        /// 待处理
        /// Pending
        /// </summary>
        Pending,

        /// <summary>
        /// 已确认
        /// Confirmed
        /// </summary>
        Confirmed,

        /// <summary>
        /// 失败
        /// Failed
        /// </summary>
        Failed
    }
}
