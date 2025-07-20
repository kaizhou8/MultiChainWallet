using System.Collections.Generic;
using System.Threading.Tasks;
using MultiChainWallet.Core.Models;

namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// 钱包仓储接口 / Wallet repository interface
    /// </summary>
    public interface IWalletRepository
    {
        /// <summary>
        /// 获取所有钱包 / Get all wallets
        /// </summary>
        Task<IEnumerable<WalletAccount>> GetAllAsync();

        /// <summary>
        /// 根据地址获取钱包 / Get wallet by address
        /// </summary>
        Task<WalletAccount> GetByAddressAsync(string address);

        /// <summary>
        /// 添加钱包 / Add wallet
        /// </summary>
        Task AddAsync(WalletAccount wallet);

        /// <summary>
        /// 更新钱包 / Update wallet
        /// </summary>
        Task UpdateAsync(WalletAccount wallet);

        /// <summary>
        /// 删除钱包 / Delete wallet
        /// </summary>
        Task DeleteAsync(string address);

        /// <summary>
        /// 检查钱包是否存在 / Check if wallet exists
        /// </summary>
        Task<bool> ExistsAsync(string address);

        /// <summary>
        /// 获取当前钱包 / Get current wallet
        /// </summary>
        Task<WalletAccount> GetCurrentWalletAsync();

        /// <summary>
        /// 设置当前钱包 / Set current wallet
        /// </summary>
        Task SetCurrentWalletAsync(string address);

        /// <summary>
        /// 批量添加钱包 / Batch add wallets
        /// </summary>
        Task AddBatchAsync(IEnumerable<WalletAccount> wallets);

        /// <summary>
        /// 根据链类型获取钱包 / Get wallets by chain type
        /// </summary>
        Task<IEnumerable<WalletAccount>> GetByChainTypeAsync(string chainType);

        /// <summary>
        /// 获取钱包总数 / Get total wallet count
        /// </summary>
        Task<int> GetTotalCountAsync();

        /// <summary>
        /// 备份钱包数据 / Backup wallet data
        /// </summary>
        Task BackupAsync(string backupPath);

        /// <summary>
        /// 从备份恢复钱包数据 / Restore wallet data from backup
        /// </summary>
        Task RestoreAsync(string backupPath, bool overwrite = false);

        /// <summary>
        /// 根据分组获取钱包 / Get wallets by group
        /// </summary>
        Task<IEnumerable<WalletAccount>> GetByGroupAsync(string group);

        /// <summary>
        /// 根据标签搜索钱包 / Search wallets by tag
        /// </summary>
        Task<IEnumerable<WalletAccount>> SearchByTagAsync(string tag);

        /// <summary>
        /// 获取所有钱包分组 / Get all wallet groups
        /// </summary>
        Task<IEnumerable<string>> GetAllGroupsAsync();

        /// <summary>
        /// 获取所有钱包标签 / Get all wallet tags
        /// </summary>
        Task<IEnumerable<string>> GetAllTagsAsync();

        /// <summary>
        /// 更新钱包使用统计 / Update wallet usage statistics
        /// </summary>
        Task UpdateUsageStatsAsync(string address);

        /// <summary>
        /// 导出钱包到JSON文件 / Export wallets to JSON file
        /// </summary>
        Task ExportToJsonAsync(string exportPath, IEnumerable<string>? addresses = null);

        /// <summary>
        /// 从JSON文件导入钱包 / Import wallets from JSON file
        /// </summary>
        Task<int> ImportFromJsonAsync(string importPath, bool overwrite = false);
    }
}
