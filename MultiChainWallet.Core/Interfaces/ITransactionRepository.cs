using System.Collections.Generic;
using System.Threading.Tasks;
using MultiChainWallet.Core.Models;

namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// 交易仓储接口 / Transaction repository interface
    /// </summary>
    public interface ITransactionRepository
    {
        /// <summary>
        /// 获取钱包的所有交易 / Get all transactions for a wallet
        /// </summary>
        Task<IEnumerable<Transaction>> GetAllForWalletAsync(string walletAddress);

        /// <summary>
        /// 根据哈希获取交易 / Get transaction by hash
        /// </summary>
        Task<Transaction> GetByHashAsync(string transactionHash);

        /// <summary>
        /// 添加交易记录 / Add transaction record
        /// </summary>
        Task AddAsync(Transaction transaction);

        /// <summary>
        /// 更新交易状态 / Update transaction status
        /// </summary>
        Task UpdateStatusAsync(string transactionHash, TransactionStatus newStatus);

        /// <summary>
        /// 获取最近的交易 / Get recent transactions
        /// </summary>
        Task<IEnumerable<Transaction>> GetRecentTransactionsAsync(string walletAddress, int count);
    }
}
