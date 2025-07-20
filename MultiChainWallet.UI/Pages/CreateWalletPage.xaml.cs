using MultiChainWallet.Core.Models;
using MultiChainWallet.Core.Services;

namespace MultiChainWallet.UI.Pages
{
    /// <summary>
    /// 创建钱包页面
    /// Create wallet page
    /// </summary>
    public partial class CreateWalletPage : ContentPage
    {
        private readonly IWalletService _walletService;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="walletService">钱包服务 / Wallet service</param>
        public CreateWalletPage(IWalletService walletService)
        {
            InitializeComponent();
            _walletService = walletService;
        }

        /// <summary>
        /// 创建钱包按钮点击事件
        /// Create wallet button click event
        /// </summary>
        private async void OnCreateWalletClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(WalletNameEntry.Text))
                {
                    await DisplayAlert("错误 / Error", "请输入钱包名称 / Please enter wallet name", "确定 / OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(PasswordEntry.Text))
                {
                    await DisplayAlert("错误 / Error", "请输入密码 / Please enter password", "确定 / OK");
                    return;
                }

                if (PasswordEntry.Text != ConfirmPasswordEntry.Text)
                {
                    await DisplayAlert("错误 / Error", "两次输入的密码不一致 / Passwords do not match", "确定 / OK");
                    return;
                }

                if (ChainTypePicker.SelectedIndex == -1)
                {
                    await DisplayAlert("错误 / Error", "请选择链类型 / Please select chain type", "确定 / OK");
                    return;
                }

                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;
                CreateWalletButton.IsEnabled = false;

                var chainType = ChainTypePicker.SelectedIndex == 0 ? ChainType.Ethereum : ChainType.Bitcoin;
                var wallet = await _walletService.CreateWalletAsync(chainType, PasswordEntry.Text);

                await DisplayAlert("成功 / Success", 
                    $"钱包创建成功 / Wallet created successfully\n地址 / Address: {wallet.Address}", 
                    "确定 / OK");

                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误 / Error", 
                    $"创建钱包失败 / Failed to create wallet: {ex.Message}", 
                    "确定 / OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
                CreateWalletButton.IsEnabled = true;
            }
        }
    }
}
