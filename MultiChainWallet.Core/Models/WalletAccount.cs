namespace MultiChainWallet.Core.Models
{
    /// <summary>
    /// 钱包账户模型
    /// Wallet account model
    /// </summary>
    public class WalletAccount
    {
        /// <summary>
        /// 钱包ID
        /// Wallet ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 钱包地址
        /// Wallet address
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// 加密后的私钥
        /// Encrypted private key
        /// </summary>
        public string EncryptedPrivateKey { get; set; } = string.Empty;

        /// <summary>
        /// 链类型
        /// Chain type
        /// </summary>
        public ChainType ChainType { get; set; }

        /// <summary>
        /// 账户名称
        /// Account name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 密码哈希
        /// Password hash
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// 是否是当前钱包
        /// Is current wallet
        /// </summary>
        public bool IsCurrent { get; set; }

        /// <summary>
        /// 钱包分组
        /// Wallet group
        /// </summary>
        public string Group { get; set; } = string.Empty;

        /// <summary>
        /// 钱包标签（以逗号分隔）
        /// Wallet tags (comma separated)
        /// </summary>
        public string Tags { get; set; } = string.Empty;

        /// <summary>
        /// 钱包元数据（JSON格式）
        /// Wallet metadata (JSON format)
        /// </summary>
        public string Metadata { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// Creation time
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 最后使用时间
        /// Last used time
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// 使用次数
        /// Usage count
        /// </summary>
        public int UsageCount { get; set; } = 0;
    }

    /// <summary>
    /// 链类型枚举
    /// Chain type enumeration
    /// </summary>
    public enum ChainType
    {
        /// <summary>
        /// 以太坊
        /// Ethereum
        /// </summary>
        Ethereum,

        /// <summary>
        /// 比特币
        /// Bitcoin
        /// </summary>
        Bitcoin
    }
}
