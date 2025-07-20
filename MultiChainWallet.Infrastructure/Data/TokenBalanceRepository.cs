using MultiChainWallet.Core.Interfaces;
using MultiChainWallet.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;

namespace MultiChainWallet.Infrastructure.Data
{
    /// <summary>
    /// 代币余额仓储类
    /// Token balance repository class
    /// </summary>
    public class TokenBalanceRepository : BaseRepository, ITokenBalanceRepository
    {
        /// <summary>
        /// 默认构造函数
        /// Default constructor
        /// </summary>
        public TokenBalanceRepository() : base()
        {
        }

        /// <summary>
        /// 带连接字符串的构造函数（用于测试）
        /// Constructor with connection string (for testing)
        /// </summary>
        /// <param name="connectionString">数据库连接字符串 / Database connection string</param>
        public TokenBalanceRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// 获取钱包的所有代币余额
        /// Get all token balances for a wallet
        /// </summary>
        public async Task<IEnumerable<TokenBalance>> GetAllForWalletAsync(string walletAddress)
        {
            const string sql = @"
                SELECT * FROM TokenBalances 
                WHERE WalletAddress = @WalletAddress AND IsEnabled = 1
                ORDER BY TokenSymbol";

            return await QueryAsync<TokenBalance>(sql, new { WalletAddress = walletAddress });
        }

        /// <summary>
        /// 获取特定代币余额
        /// Get specific token balance
        /// </summary>
        public async Task<TokenBalance> GetTokenBalanceAsync(
            string walletAddress,
            string tokenContractAddress)
        {
            const string sql = @"
                SELECT * FROM TokenBalances 
                WHERE WalletAddress = @WalletAddress 
                AND TokenContractAddress = @TokenContractAddress";

            return await QuerySingleOrDefaultAsync<TokenBalance>(sql, new 
            { 
                WalletAddress = walletAddress,
                TokenContractAddress = tokenContractAddress
            });
        }

        /// <summary>
        /// 添加代币余额
        /// Add token balance
        /// </summary>
        public async Task AddAsync(TokenBalance tokenBalance)
        {
            const string sql = @"
                INSERT INTO TokenBalances (
                    WalletAddress, TokenContractAddress, TokenSymbol, 
                    TokenName, Balance, Decimals, IsEnabled
                )
                VALUES (
                    @WalletAddress, @TokenContractAddress, @TokenSymbol, 
                    @TokenName, @Balance, @Decimals, @IsEnabled
                )";

            await ExecuteAsync(sql, tokenBalance);
        }

        /// <summary>
        /// 更新代币余额
        /// Update token balance
        /// </summary>
        public async Task UpdateBalanceAsync(TokenBalance tokenBalance)
        {
            const string sql = @"
                UPDATE TokenBalances 
                SET Balance = @Balance,
                    TokenSymbol = @TokenSymbol,
                    TokenName = @TokenName,
                    Decimals = @Decimals,
                    IsEnabled = @IsEnabled
                WHERE WalletAddress = @WalletAddress 
                AND TokenContractAddress = @TokenContractAddress";

            await ExecuteAsync(sql, tokenBalance);
        }

        /// <summary>
        /// 删除代币余额
        /// Delete token balance
        /// </summary>
        public async Task DeleteAsync(string walletAddress, string tokenContractAddress)
        {
            const string sql = @"
                DELETE FROM TokenBalances 
                WHERE WalletAddress = @WalletAddress 
                AND TokenContractAddress = @TokenContractAddress";

            await ExecuteAsync(sql, new
            {
                WalletAddress = walletAddress,
                TokenContractAddress = tokenContractAddress
            });
        }

        /// <summary>
        /// 更新代币余额数量
        /// Update token balance amount
        /// </summary>
        public async Task UpdateTokenBalanceAmountAsync(
            string walletAddress,
            string tokenContractAddress,
            string balance)
        {
            const string sql = @"
                UPDATE TokenBalances 
                SET Balance = @Balance
                WHERE WalletAddress = @WalletAddress 
                AND TokenContractAddress = @TokenContractAddress";

            await ExecuteAsync(sql, new
            {
                WalletAddress = walletAddress,
                TokenContractAddress = tokenContractAddress,
                Balance = balance
            });
        }

        /// <summary>
        /// 启用或禁用代币
        /// Enable or disable token
        /// </summary>
        public async Task EnableTokenAsync(
            string walletAddress,
            string tokenContractAddress,
            bool isEnabled)
        {
            const string sql = @"
                UPDATE TokenBalances 
                SET IsEnabled = @IsEnabled
                WHERE WalletAddress = @WalletAddress 
                AND TokenContractAddress = @TokenContractAddress";

            await ExecuteAsync(sql, new
            {
                WalletAddress = walletAddress,
                TokenContractAddress = tokenContractAddress,
                IsEnabled = isEnabled
            });
        }
    }
}
