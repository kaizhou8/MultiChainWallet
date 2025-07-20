using MultiChainWallet.Core.Interfaces;
using MultiChainWallet.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultiChainWallet.Infrastructure.Data
{
    /// <summary>
    /// 交易仓储类
    /// Transaction repository class
    /// </summary>
    public class TransactionRepository : BaseRepository, ITransactionRepository
    {
        /// <summary>
        /// 默认构造函数
        /// Default constructor
        /// </summary>
        public TransactionRepository() : base()
        {
        }

        /// <summary>
        /// 带连接字符串的构造函数（用于测试）
        /// Constructor with connection string (for testing)
        /// </summary>
        /// <param name="connectionString">数据库连接字符串 / Database connection string</param>
        public TransactionRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// 获取钱包的所有交易
        /// Get all transactions for a wallet
        /// </summary>
        public async Task<IEnumerable<Transaction>> GetAllForWalletAsync(string walletAddress)
        {
            const string sql = @"
                SELECT * FROM Transactions 
                WHERE FromAddress = @Address OR ToAddress = @Address
                ORDER BY TransactionTime DESC";

            return await QueryAsync<Transaction>(sql, new { Address = walletAddress });
        }

        /// <summary>
        /// 根据哈希获取交易
        /// Get transaction by hash
        /// </summary>
        public async Task<Transaction> GetByHashAsync(string transactionHash)
        {
            const string sql = "SELECT * FROM Transactions WHERE Hash = @Hash";
            return await QuerySingleOrDefaultAsync<Transaction>(sql, new { Hash = transactionHash });
        }

        /// <summary>
        /// 添加交易
        /// Add transaction
        /// </summary>
        public async Task AddAsync(Transaction transaction)
        {
            const string sql = @"
                INSERT INTO Transactions (
                    Hash, FromAddress, ToAddress, Amount, TokenContractAddress,
                    TransactionTime, Status, ChainType, GasPrice, GasLimit,
                    GasUsed, BlockHeight, Confirmations, Note
                )
                VALUES (
                    @Hash, @FromAddress, @ToAddress, @Amount, @TokenContractAddress,
                    @TransactionTime, @Status, @ChainType, @GasPrice, @GasLimit,
                    @GasUsed, @BlockHeight, @Confirmations, @Note
                )";
            await ExecuteAsync(sql, transaction);
        }

        /// <summary>
        /// 更新交易状态
        /// Update transaction status
        /// </summary>
        public async Task UpdateStatusAsync(
            string transactionHash, 
            TransactionStatus newStatus)
        {
            const string sql = @"
                UPDATE Transactions 
                SET Status = @Status
                WHERE Hash = @Hash";

            await ExecuteAsync(sql, new 
            { 
                Hash = transactionHash, 
                Status = newStatus.ToString()
            });
        }

        /// <summary>
        /// 获取最近的交易
        /// Get recent transactions
        /// </summary>
        public async Task<IEnumerable<Transaction>> GetRecentTransactionsAsync(
            string walletAddress, 
            int count)
        {
            const string sql = @"
                SELECT * FROM Transactions 
                WHERE FromAddress = @Address OR ToAddress = @Address
                ORDER BY TransactionTime DESC
                LIMIT @Count";

            return await QueryAsync<Transaction>(sql, new { Address = walletAddress, Count = count });
        }

        /// <summary>
        /// 更新交易确认数
        /// Update transaction confirmations
        /// </summary>
        public async Task UpdateTransactionConfirmationsAsync(
            string hash, 
            int confirmations)
        {
            const string sql = @"
                UPDATE Transactions 
                SET Confirmations = @Confirmations
                WHERE Hash = @Hash";

            await ExecuteAsync(sql, new { Hash = hash, Confirmations = confirmations });
        }

        /// <summary>
        /// 获取交易数量
        /// Get transaction count
        /// </summary>
        public async Task<int> GetTransactionCountAsync(string address)
        {
            const string sql = @"
                SELECT COUNT(*) FROM Transactions 
                WHERE FromAddress = @Address OR ToAddress = @Address";

            return await QuerySingleOrDefaultAsync<int>(sql, new { Address = address });
        }
    }
}
