using System.Threading.Tasks;
using MultiChainWallet.Core.Models;

namespace MultiChainWallet.Core.Services
{
    /// <summary>
    /// 钱包仓储接口
    /// Wallet repository interface
    /// </summary>
    public interface IWalletRepository
    {
        /// <summary>
        /// 获取当前钱包
        /// Get current wallet
        /// </summary>
        /// <returns>钱包信息 / Wallet information</returns>
        Task<Wallet> GetCurrentWalletAsync();

        /// <summary>
        /// 更新钱包信息
        /// Update wallet information
        /// </summary>
        /// <param name="wallet">钱包信息 / Wallet information</param>
        Task UpdateWalletAsync(Wallet wallet);
    }
}
