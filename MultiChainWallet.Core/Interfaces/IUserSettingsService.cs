using System.Threading.Tasks;
using MultiChainWallet.Core.Models;

namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// 用户设置服务接口
    /// User settings service interface
    /// </summary>
    public interface IUserSettingsService
    {
        /// <summary>
        /// 获取安全设置
        /// Get security settings
        /// </summary>
        Task<SecuritySettings> GetSecuritySettingsAsync();

        /// <summary>
        /// 保存安全设置
        /// Save security settings
        /// </summary>
        /// <param name="settings">安全设置 / Security settings</param>
        Task SaveSecuritySettingsAsync(SecuritySettings settings);

        /// <summary>
        /// 获取密码哈希
        /// Get password hash
        /// </summary>
        Task<string> GetPasswordHashAsync();

        /// <summary>
        /// 保存密码哈希
        /// Save password hash
        /// </summary>
        /// <param name="passwordHash">密码哈希 / Password hash</param>
        Task SavePasswordHashAsync(string passwordHash);
    }
} 