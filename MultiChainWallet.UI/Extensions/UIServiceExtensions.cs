using Microsoft.Extensions.DependencyInjection;
using MultiChainWallet.UI.Pages;
using MultiChainWallet.UI.ViewModels;

namespace MultiChainWallet.UI.Extensions
{
    /// <summary>
    /// UI服务扩展方法
    /// UI service extension methods
    /// </summary>
    public static class UIServiceExtensions
    {
        /// <summary>
        /// 添加UI相关服务
        /// Add UI-related services
        /// </summary>
        public static IServiceCollection AddUIServices(this IServiceCollection services)
        {
            // 注册UI组件 / Register UI components
            services.AddTransient<SecuritySettingsViewModel>();
            services.AddTransient<SecuritySettingsPage>();
            services.AddTransient<TransactionConfirmationViewModel>();
            services.AddTransient<TransactionConfirmationPage>();

            return services;
        }
    }
} 