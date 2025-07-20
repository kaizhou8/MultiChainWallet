using System.Collections.Generic;
using System.Threading.Tasks;
using MultiChainWallet.Core.Models;

namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// 代币余额仓储接口 / Token balance repository interface
    /// </summary>
    public interface ITokenBalanceRepository
    {
        /// <summary>
        /// 获取钱包的所有代币余额 / Get all token balances for a wallet
        /// </summary>
        Task<IEnumerable<TokenBalance>> GetAllForWalletAsync(string walletAddress);

        /// <summary>
        /// 获取特定代币余额 / Get specific token balance
        /// </summary>
        Task<TokenBalance> GetTokenBalanceAsync(string walletAddress, string tokenContractAddress);

        /// <summary>
        /// 更新代币余额 / Update token balance
        /// </summary>
        Task UpdateBalanceAsync(TokenBalance tokenBalance);

        /// <summary>
        /// 添加代币余额记录 / Add token balance record
        /// </summary>
        Task AddAsync(TokenBalance tokenBalance);

        /// <summary>
        /// 删除代币余额记录 / Delete token balance record
        /// </summary>
        Task DeleteAsync(string walletAddress, string tokenContractAddress);
    }
}
