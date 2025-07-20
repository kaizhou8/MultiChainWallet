using MultiChainWallet.Core.Models;
using MultiChainWallet.Core.Services;
using System.Diagnostics;

namespace MultiChainWallet.UI.Pages
{
    /// <summary>
    /// 钱包详情页面
    /// Wallet details page
    /// </summary>
    [QueryProperty(nameof(WalletAddress), "address")]
    public partial class WalletDetailsPage : ContentPage
    {
        private readonly IWalletService _walletService;
        private WalletAccount _wallet;
        private string _walletAddress;

        /// <summary>
        /// 钱包地址
        /// Wallet address
        /// </summary>
        public string WalletAddress
        {
            get => _walletAddress;
            set
            {
                _walletAddress = value;
                LoadWalletDetails();
            }
        }

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="walletService">钱包服务 / Wallet service</param>
        public WalletDetailsPage(IWalletService walletService)
        {
            InitializeComponent();
            _walletService = walletService;
        }

        /// <summary>
        /// 加载钱包详情
        /// Load wallet details
        /// </summary>
        private async void LoadWalletDetails()
        {
            if (string.IsNullOrEmpty(WalletAddress))
                return;

            try
            {
                var wallets = await _walletService.GetWalletsAsync();
                _wallet = wallets.FirstOrDefault(w => w.Address == WalletAddress);

                if (_wallet == null)
                {
                    await DisplayAlert("错误 / Error", 
                        "找不到钱包 / Wallet not found", 
                        "确定 / OK");
                    await Shell.Current.GoToAsync("..");
                    return;
                }

                // 更新钱包使用统计
                // Update wallet usage statistics
                await _walletService.UpdateWalletUsageStatsAsync(_wallet.Address);

                // 更新UI
                // Update UI
                UpdateUI();
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误 / Error", 
                    $"加载钱包详情失败 / Failed to load wallet details: {ex.Message}", 
                    "确定 / OK");
                Debug.WriteLine($"加载钱包详情失败 / Failed to load wallet details: {ex}");
            }
        }

        /// <summary>
        /// 更新UI
        /// Update UI
        /// </summary>
        private async void UpdateUI()
        {
            try
            {
                // 基本信息
                // Basic information
                NameLabel.Text = _wallet.Name;
                AddressLabel.Text = _wallet.Address;
                ChainTypeLabel.Text = _wallet.ChainType.ToString();

                // 获取余额
                // Get balance
                var balance = await _walletService.GetBalanceAsync(_wallet.Address, _wallet.ChainType);
                BalanceLabel.Text = $"{balance} {(_wallet.ChainType == ChainType.Ethereum ? "ETH" : "BTC")}";

                // 组和标签
                // Group and tags
                GroupEntry.Text = _wallet.Group;
                TagsEntry.Text = _wallet.Tags;

                // 使用统计
                // Usage statistics
                CreatedAtLabel.Text = _wallet.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                LastUsedLabel.Text = _wallet.LastUsedAt.HasValue 
                    ? _wallet.LastUsedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") 
                    : "从未使用 / Never used";
                UsageCountLabel.Text = _wallet.UsageCount.ToString();
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误 / Error", 
                    $"更新UI失败 / Failed to update UI: {ex.Message}", 
                    "确定 / OK");
                Debug.WriteLine($"更新UI失败 / Failed to update UI: {ex}");
            }
        }

        /// <summary>
        /// 复制地址按钮点击事件
        /// Copy address button click event
        /// </summary>
        private async void OnCopyAddressClicked(object sender, EventArgs e)
        {
            await Clipboard.SetTextAsync(_wallet.Address);
            await DisplayAlert("成功 / Success", 
                "地址已复制到剪贴板 / Address copied to clipboard", 
                "确定 / OK");
        }

        /// <summary>
        /// 更新组按钮点击事件
        /// Update group button click event
        /// </summary>
        private async void OnUpdateGroupClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await _walletService.UpdateWalletGroupAsync(_wallet.Address, GroupEntry.Text);
                if (result)
                {
                    _wallet.Group = GroupEntry.Text;
                    await DisplayAlert("成功 / Success", 
                        "钱包组已更新 / Wallet group updated", 
                        "确定 / OK");
                }
                else
                {
                    await DisplayAlert("错误 / Error", 
                        "更新钱包组失败 / Failed to update wallet group", 
                        "确定 / OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误 / Error", 
                    $"更新钱包组失败 / Failed to update wallet group: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 更新标签按钮点击事件
        /// Update tags button click event
        /// </summary>
        private async void OnUpdateTagsClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await _walletService.UpdateWalletTagsAsync(_wallet.Address, TagsEntry.Text);
                if (result)
                {
                    _wallet.Tags = TagsEntry.Text;
                    await DisplayAlert("成功 / Success", 
                        "钱包标签已更新 / Wallet tags updated", 
                        "确定 / OK");
                }
                else
                {
                    await DisplayAlert("错误 / Error", 
                        "更新钱包标签失败 / Failed to update wallet tags", 
                        "确定 / OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误 / Error", 
                    $"更新钱包标签失败 / Failed to update wallet tags: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 发送交易按钮点击事件
        /// Send transaction button click event
        /// </summary>
        private async void OnSendTransactionClicked(object sender, EventArgs e)
        {
            var navigationParameter = new Dictionary<string, object>
            {
                { "address", _wallet.Address }
            };

            await Shell.Current.GoToAsync("SendTransactionPage", navigationParameter);
        }

        /// <summary>
        /// 刷新按钮点击事件
        /// Refresh button click event
        /// </summary>
        private void OnRefreshClicked(object sender, EventArgs e)
        {
            LoadWalletDetails();
        }
    }
}
