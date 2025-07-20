using System.Collections.Generic;
using System.Threading.Tasks;
using MultiChainWallet.Core.Models;

namespace MultiChainWallet.Core.Services
{
    /// <summary>
    /// 钱包服务接口
    /// Wallet service interface
    /// </summary>
    public interface IWalletService
    {
        /// <summary>
        /// 创建新钱包
        /// Create a new wallet
        /// </summary>
        /// <param name="chainType">链类型 / Chain type</param>
        /// <param name="password">密码 / Password</param>
        /// <returns>钱包账户 / Wallet account</returns>
        Task<WalletAccount> CreateWalletAsync(ChainType chainType, string password);

        /// <summary>
        /// 获取所有钱包
        /// Get all wallets
        /// </summary>
        /// <returns>钱包列表 / Wallet list</returns>
        Task<IEnumerable<WalletAccount>> GetWalletsAsync();

        /// <summary>
        /// 获取钱包余额
        /// Get wallet balance
        /// </summary>
        /// <param name="address">钱包地址 / Wallet address</param>
        /// <param name="chainType">链类型 / Chain type</param>
        /// <returns>余额 / Balance</returns>
        Task<decimal> GetBalanceAsync(string address, ChainType chainType);

        /// <summary>
        /// 发送交易
        /// Send transaction
        /// </summary>
        /// <param name="fromAddress">发送地址 / From address</param>
        /// <param name="toAddress">接收地址 / To address</param>
        /// <param name="amount">金额 / Amount</param>
        /// <param name="password">密码 / Password</param>
        /// <param name="chainType">链类型 / Chain type</param>
        /// <returns>交易哈希 / Transaction hash</returns>
        Task<string> SendTransactionAsync(string fromAddress, string toAddress, decimal amount, string password, ChainType chainType);

        /// <summary>
        /// 验证密码
        /// Validate password
        /// </summary>
        /// <param name="password">密码 / Password</param>
        /// <returns>是否有效 / Is valid</returns>
        Task<bool> ValidatePasswordAsync(string password);

        /// <summary>
        /// 修改密码
        /// Change password
        /// </summary>
        /// <param name="oldPassword">旧密码 / Old password</param>
        /// <param name="newPassword">新密码 / New password</param>
        /// <returns>是否成功 / Success</returns>
        Task<bool> ChangePasswordAsync(string oldPassword, string newPassword);

        /// <summary>
        /// 备份钱包
        /// Backup wallet
        /// </summary>
        /// <param name="backupPath">备份路径 / Backup path</param>
        /// <param name="password">密码 / Password</param>
        /// <returns>是否成功 / Success</returns>
        Task<bool> BackupWalletAsync(string backupPath, string password);

        /// <summary>
        /// 按组获取钱包
        /// Get wallets by group
        /// </summary>
        /// <param name="group">组名 / Group name</param>
        /// <returns>钱包列表 / Wallet list</returns>
        Task<IEnumerable<WalletAccount>> GetWalletsByGroupAsync(string group);

        /// <summary>
        /// 获取所有钱包组
        /// Get all wallet groups
        /// </summary>
        /// <returns>组名列表 / Group name list</returns>
        Task<IEnumerable<string>> GetAllGroupsAsync();

        /// <summary>
        /// 按标签搜索钱包
        /// Search wallets by tag
        /// </summary>
        /// <param name="tag">标签 / Tag</param>
        /// <returns>钱包列表 / Wallet list</returns>
        Task<IEnumerable<WalletAccount>> SearchWalletsByTagAsync(string tag);

        /// <summary>
        /// 获取所有钱包标签
        /// Get all wallet tags
        /// </summary>
        /// <returns>标签列表 / Tag list</returns>
        Task<IEnumerable<string>> GetAllTagsAsync();

        /// <summary>
        /// 更新钱包组
        /// Update wallet group
        /// </summary>
        /// <param name="address">钱包地址 / Wallet address</param>
        /// <param name="group">组名 / Group name</param>
        /// <returns>是否成功 / Success</returns>
        Task<bool> UpdateWalletGroupAsync(string address, string group);

        /// <summary>
        /// 更新钱包标签
        /// Update wallet tags
        /// </summary>
        /// <param name="address">钱包地址 / Wallet address</param>
        /// <param name="tags">标签列表（逗号分隔） / Tag list (comma separated)</param>
        /// <returns>是否成功 / Success</returns>
        Task<bool> UpdateWalletTagsAsync(string address, string tags);

        /// <summary>
        /// 导出钱包到JSON文件
        /// Export wallets to JSON file
        /// </summary>
        /// <param name="filePath">文件路径 / File path</param>
        /// <returns>是否成功 / Success</returns>
        Task<bool> ExportWalletsToJsonAsync(string filePath);

        /// <summary>
        /// 从JSON文件导入钱包
        /// Import wallets from JSON file
        /// </summary>
        /// <param name="filePath">文件路径 / File path</param>
        /// <param name="overwrite">是否覆盖现有钱包 / Whether to overwrite existing wallets</param>
        /// <returns>导入的钱包数量 / Number of imported wallets</returns>
        Task<int> ImportWalletsFromJsonAsync(string filePath, bool overwrite = false);

        /// <summary>
        /// 更新钱包使用统计
        /// Update wallet usage statistics
        /// </summary>
        /// <param name="address">钱包地址 / Wallet address</param>
        /// <returns>是否成功 / Success</returns>
        Task<bool> UpdateWalletUsageStatsAsync(string address);
    }
}
