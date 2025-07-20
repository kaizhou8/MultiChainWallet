namespace MultiChainWallet.Core.Models
{
    /// <summary>
    /// 钱包模型
    /// Wallet model
    /// </summary>
    public class Wallet
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
        public string Address { get; set; }

        /// <summary>
        /// 密码哈希
        /// Password hash
        /// </summary>
        public string PasswordHash { get; set; }

        /// <summary>
        /// 加密的私钥
        /// Encrypted private key
        /// </summary>
        public string EncryptedPrivateKey { get; set; }

        /// <summary>
        /// 链类型
        /// Chain type
        /// </summary>
        public ChainType ChainType { get; set; }

        /// <summary>
        /// 是否是当前钱包
        /// Is current wallet
        /// </summary>
        public bool IsCurrent { get; set; }
    }
}
