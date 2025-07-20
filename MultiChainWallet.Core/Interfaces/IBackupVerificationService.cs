using System.Threading.Tasks;

namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// 备份验证服务接口 / Backup verification service interface
    /// </summary>
    public interface IBackupVerificationService
    {
        /// <summary>
        /// 验证备份数据完整性 / Verify backup data integrity
        /// </summary>
        Task<bool> VerifyDataIntegrityAsync(string backupPath);

        /// <summary>
        /// 验证备份数据结构 / Verify backup data structure
        /// </summary>
        Task<bool> VerifyDataStructureAsync(string backupPath);

        /// <summary>
        /// 验证版本兼容性 / Verify version compatibility
        /// </summary>
        Task<bool> VerifyVersionCompatibilityAsync(string backupPath);
    }
}
