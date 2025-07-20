using System;
using System.Collections.Generic;

namespace MultiChainWallet.Core.Models
{
    /// <summary>
    /// 备份数据类
    /// Backup data class
    /// </summary>
    public class BackupData
    {
        /// <summary>
        /// 创建时间
        /// Creation time
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 版本
        /// Version
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// 描述
        /// Description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 钱包账户列表
        /// List of wallet accounts
        /// </summary>
        public List<WalletAccount> WalletAccounts { get; set; } = new();

        /// <summary>
        /// 代币余额列表
        /// List of token balances
        /// </summary>
        public List<TokenBalance> TokenBalances { get; set; } = new();

        /// <summary>
        /// 交易列表
        /// List of transactions
        /// </summary>
        public List<Transaction> Transactions { get; set; } = new();

        /// <summary>
        /// 校验和
        /// Checksum
        /// </summary>
        public string Checksum { get; set; } = string.Empty;
    }
}
