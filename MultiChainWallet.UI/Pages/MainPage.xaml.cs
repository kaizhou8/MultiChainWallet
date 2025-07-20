using Microsoft.Maui.Controls;
using MultiChainWallet.Core.Services;
using MultiChainWallet.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace MultiChainWallet.UI.Pages
{
    /// <summary>
    /// 主页面
    /// Main page
    /// </summary>
    public partial class MainPage : ContentPage
    {
        private readonly IWalletService _walletService;
        private readonly HardwareWalletManager _hardwareWalletManager;
        private readonly ILogger<MainPage> _logger;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public MainPage(
            IWalletService walletService, 
            HardwareWalletManager hardwareWalletManager,
            ILogger<MainPage> logger)
        {
            InitializeComponent();
            _walletService = walletService;
            _hardwareWalletManager = hardwareWalletManager;
            _logger = logger;
        }

        /// <summary>
        /// 创建钱包按钮点击事件
        /// Create wallet button click event
        /// </summary>
        private async void OnCreateWalletClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CreateWalletPage(_walletService));
        }

        /// <summary>
        /// 查看钱包按钮点击事件
        /// View wallets button click event
        /// </summary>
        private async void OnViewWalletsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new WalletListPage(_walletService));
        }

        /// <summary>
        /// 发送交易按钮点击事件
        /// Send transaction button click event
        /// </summary>
        private async void OnSendTransactionClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SendTransactionPage(_walletService));
        }

        /// <summary>
        /// 连接硬件钱包按钮点击事件
        /// Connect hardware wallet button click event
        /// </summary>
        private async void OnConnectHardwareWalletClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new HardwareWalletConnectionPage(_hardwareWalletManager, _logger));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导航到硬件钱包连接页面时出错 / Error navigating to hardware wallet connection page");
                await DisplayAlert("错误 / Error", 
                    $"导航到硬件钱包连接页面时出错: {ex.Message} / Error navigating to hardware wallet connection page: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 硬件钱包交易按钮点击事件
        /// Hardware wallet transaction button click event
        /// </summary>
        private async void OnHardwareWalletTransactionClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new HardwareWalletTransactionPage(_hardwareWalletManager, _walletService, _logger));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导航到硬件钱包交易页面时出错 / Error navigating to hardware wallet transaction page");
                await DisplayAlert("错误 / Error", 
                    $"导航到硬件钱包交易页面时出错: {ex.Message} / Error navigating to hardware wallet transaction page: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 硬件钱包地址按钮点击事件
        /// Hardware wallet address button click event
        /// </summary>
        private async void OnHardwareWalletAddressClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new HardwareWalletAddressPage(_hardwareWalletManager, _walletService, _logger));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导航到硬件钱包地址页面时出错 / Error navigating to hardware wallet address page");
                await DisplayAlert("错误 / Error", 
                    $"导航到硬件钱包地址页面时出错: {ex.Message} / Error navigating to hardware wallet address page: {ex.Message}", 
                    "确定 / OK");
            }
        }
    }
}
