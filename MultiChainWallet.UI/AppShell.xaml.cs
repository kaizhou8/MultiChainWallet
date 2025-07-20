using MultiChainWallet.UI.Pages;

namespace MultiChainWallet.UI
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // 注册路由
            // Register routes
            Routing.RegisterRoute("CreateWalletPage", typeof(Pages.CreateWalletPage));
            Routing.RegisterRoute("AddTokenPage", typeof(Pages.AddTokenPage));
            Routing.RegisterRoute("SendTransactionPage", typeof(Pages.SendTransactionPage));
            Routing.RegisterRoute("SendTokenPage", typeof(Pages.SendTokenPage));
            Routing.RegisterRoute("TransactionHistoryPage", typeof(Pages.TransactionHistoryPage));
            Routing.RegisterRoute("SettingsPage", typeof(Pages.SettingsPage));
            Routing.RegisterRoute("ChangePasswordPage", typeof(Pages.ChangePasswordPage));
            Routing.RegisterRoute("BackupWalletPage", typeof(Pages.BackupWalletPage));
            Routing.RegisterRoute("TransactionDetailsPage", typeof(Pages.TransactionDetailsPage));
            Routing.RegisterRoute("BiometricSettingsPage", typeof(Pages.BiometricSettingsPage));
            
            // 注册新增的路由
            // Register new routes
            Routing.RegisterRoute("WalletDetailsPage", typeof(Pages.WalletDetailsPage));
            Routing.RegisterRoute("WalletGroupsPage", typeof(Pages.WalletGroupsPage));
            Routing.RegisterRoute("WalletTagsPage", typeof(Pages.WalletTagsPage));
            Routing.RegisterRoute("WalletImportExportPage", typeof(Pages.WalletImportExportPage));
            
            // 注册市场数据页面路由
            // Register market data page route
            Routing.RegisterRoute("MarketDataPage", typeof(Pages.MarketDataPage));
            Routing.RegisterRoute("CryptoDetailsPage", typeof(Pages.CryptoDetailsPage));
        }
    }
}
