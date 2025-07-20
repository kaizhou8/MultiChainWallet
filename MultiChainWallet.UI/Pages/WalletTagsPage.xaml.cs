using MultiChainWallet.Core.Models;
using MultiChainWallet.Core.Services;
using System.Collections.ObjectModel;

namespace MultiChainWallet.UI.Pages
{
    /// <summary>
    /// 钱包标签管理页面
    /// Wallet tags management page
    /// </summary>
    public partial class WalletTagsPage : ContentPage
    {
        private readonly IWalletService _walletService;
        private ObservableCollection<TagViewModel> _tags;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="walletService">钱包服务 / Wallet service</param>
        public WalletTagsPage(IWalletService walletService)
        {
            InitializeComponent();
            _walletService = walletService;
            _tags = new ObservableCollection<TagViewModel>();
            TagsCollection.ItemsSource = _tags;
            LoadTags();
        }

        /// <summary>
        /// 加载钱包标签
        /// Load wallet tags
        /// </summary>
        private async void LoadTags()
        {
            try
            {
                var tags = await _walletService.GetAllTagsAsync();
                _tags.Clear();

                foreach (var tag in tags)
                {
                    var wallets = await _walletService.SearchWalletsByTagAsync(tag);
                    _tags.Add(new TagViewModel
                    {
                        Name = tag,
                        WalletCount = wallets.Count()
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误 / Error", 
                    $"加载钱包标签失败 / Failed to load wallet tags: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 搜索标签按钮点击事件
        /// Search tag button click event
        /// </summary>
        private async void OnSearchTagClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TagSearchEntry.Text))
            {
                LoadTags();
                return;
            }

            try
            {
                var searchText = TagSearchEntry.Text.Trim().ToLower();
                var allTags = await _walletService.GetAllTagsAsync();
                var filteredTags = allTags.Where(t => t.ToLower().Contains(searchText));
                
                _tags.Clear();
                foreach (var tag in filteredTags)
                {
                    var wallets = await _walletService.SearchWalletsByTagAsync(tag);
                    _tags.Add(new TagViewModel
                    {
                        Name = tag,
                        WalletCount = wallets.Count()
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误 / Error", 
                    $"搜索标签失败 / Failed to search tags: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 查看标签按钮点击事件
        /// View tag button click event
        /// </summary>
        private async void OnViewTagClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string tagName)
            {
                var navigationParameter = new Dictionary<string, object>
                {
                    { "TagName", tagName }
                };

                await Shell.Current.GoToAsync("WalletListPage", navigationParameter);
            }
        }

        /// <summary>
        /// 刷新按钮点击事件
        /// Refresh button click event
        /// </summary>
        private void OnRefreshClicked(object sender, EventArgs e)
        {
            LoadTags();
        }
    }

    /// <summary>
    /// 标签视图模型
    /// Tag view model
    /// </summary>
    public class TagViewModel
    {
        /// <summary>
        /// 标签名
        /// Tag name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 钱包数量
        /// Wallet count
        /// </summary>
        public int WalletCount { get; set; }
    }
}
