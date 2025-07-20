using System.Threading.Tasks;

namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// 备份服务接口 / Backup service interface
    /// </summary>
    public interface IBackupService
    {
        /// <summary>
        /// 创建备份 / Create backup
        /// </summary>
        Task<string> CreateBackupAsync(string password);

        /// <summary>
        /// 从备份恢复 / Restore from backup
        /// </summary>
        Task RestoreFromBackupAsync(string backupPath, string password);

        /// <summary>
        /// 验证备份 / Verify backup
        /// </summary>
        Task<bool> VerifyBackupAsync(string backupPath);
    }
}
