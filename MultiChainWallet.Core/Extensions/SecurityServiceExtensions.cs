using Microsoft.Extensions.DependencyInjection;
using MultiChainWallet.Core.Interfaces;
using MultiChainWallet.Core.Services.Security;

namespace MultiChainWallet.Core.Extensions
{
    /// <summary>
    /// 安全服务扩展方法
    /// Security service extension methods
    /// </summary>
    public static class SecurityServiceExtensions
    {
        /// <summary>
        /// 添加安全相关服务
        /// Add security-related services
        /// </summary>
        public static IServiceCollection AddSecurityServices(this IServiceCollection services)
        {
            // 注册核心服务 / Register core services
            services.AddSingleton<ISecurityService, SecurityService>();
            services.AddSingleton<IBiometricAuthService, WindowsHelloBiometricService>();
            services.AddSingleton<ITotpService, TotpService>();
            services.AddSingleton<IMultiFactorAuthManager, MultiFactorAuthManager>();
            services.AddSingleton<IUserSettingsService, UserSettingsService>();
            services.AddSingleton<ITransactionConfirmationService, TransactionConfirmationService>();

            // 注册自定义安全服务 / Register custom security services
            services.AddSingleton<CustomSecurityService>();
            services.AddSingleton<IntegrityVerifier>();

            return services;
        }
    }
} 