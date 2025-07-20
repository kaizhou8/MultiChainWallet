using MultiChainWallet.Core.Models;
using MultiChainWallet.Core.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MultiChainWallet.UI.Pages
{
    /// <summary>
    /// 钱包列表页面
    /// Wallet list page
    /// </summary>
    [QueryProperty(nameof(GroupName), "GroupName")]
    [QueryProperty(nameof(TagName), "TagName")]
    public partial class WalletListPage : ContentPage
    {
        private readonly IWalletService _walletService;
        private ObservableCollection<WalletViewModel> _wallets;
        private string _groupName;
        private string _tagName;

        /// <summary>
        /// 组名
        /// Group name
        /// </summary>
        public string GroupName
        {
            get => _groupName;
            set
            {
                _groupName = value;
                if (!string.IsNullOrEmpty(value))
                {
                    LoadWalletsByGroup(value);
                }
            }
        }

        /// <summary>
        /// 标签名
        /// Tag name
        /// </summary>
        public string TagName
        {
            get => _tagName;
            set
            {
                _tagName = value;
                if (!string.IsNullOrEmpty(value))
                {
                    LoadWalletsByTag(value);
                }
            }
        }

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="walletService">钱包服务 / Wallet service</param>
        public WalletListPage(IWalletService walletService)
        {
            InitializeComponent();
            _walletService = walletService;
            _wallets = new ObservableCollection<WalletViewModel>();
            WalletsCollection.ItemsSource = _wallets;
            LoadFilters();
            LoadWallets();
        }

        /// <summary>
        /// 加载过滤器
        /// Load filters
        /// </summary>
        private async void LoadFilters()
        {
            try
            {
                // 加载组过滤器
                // Load group filter
                var groups = await _walletService.GetAllGroupsAsync();
                var groupList = new List<string> { "全部 / All" };
                groupList.AddRange(groups);
                GroupFilterPicker.ItemsSource = groupList;
                GroupFilterPicker.SelectedIndex = 0;

                // 加载标签过滤器
                // Load tag filter
                var tags = await _walletService.GetAllTagsAsync();
                var tagList = new List<string> { "全部 / All" };
                tagList.AddRange(tags);
                TagFilterPicker.ItemsSource = tagList;
                TagFilterPicker.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误 / Error", 
                    $"加载过滤器失败 / Failed to load filters: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 加载钱包列表
        /// Load wallet list
        /// </summary>
        private async void LoadWallets()
        {
            try
            {
                var wallets = await _walletService.GetWalletsAsync();
                await UpdateWalletList(wallets);
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误 / Error", 
                    $"加载钱包列表失败 / Failed to load wallet list: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 按组加载钱包
        /// Load wallets by group
        /// </summary>
        private async void LoadWalletsByGroup(string group)
        {
            try
            {
                var wallets = await _walletService.GetWalletsByGroupAsync(group);
                await UpdateWalletList(wallets);

                // 更新组过滤器
                // Update group filter
                var groups = await _walletService.GetAllGroupsAsync();
                var groupList = new List<string> { "全部 / All" };
                groupList.AddRange(groups);
                GroupFilterPicker.ItemsSource = groupList;
                GroupFilterPicker.SelectedItem = group;
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误 / Error", 
                    $"按组加载钱包失败 / Failed to load wallets by group: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 按标签加载钱包
        /// Load wallets by tag
        /// </summary>
        private async void LoadWalletsByTag(string tag)
        {
            try
            {
                var wallets = await _walletService.SearchWalletsByTagAsync(tag);
                await UpdateWalletList(wallets);

                // 更新标签过滤器
                // Update tag filter
                var tags = await _walletService.GetAllTagsAsync();
                var tagList = new List<string> { "全部 / All" };
                tagList.AddRange(tags);
                TagFilterPicker.ItemsSource = tagList;
                TagFilterPicker.SelectedItem = tag;
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误 / Error", 
                    $"按标签加载钱包失败 / Failed to load wallets by tag: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 更新钱包列表
        /// Update wallet list
        /// </summary>
        private async Task UpdateWalletList(IEnumerable<WalletAccount> wallets)
        {
            _wallets.Clear();

            foreach (var wallet in wallets)
            {
                var balance = await _walletService.GetBalanceAsync(wallet.Address, wallet.ChainType);
                _wallets.Add(new WalletViewModel
                {
                    Name = wallet.Name,
                    Address = wallet.Address,
                    ChainType = wallet.ChainType.ToString(),
                    Balance = $"{balance} {(wallet.ChainType == ChainType.Ethereum ? "ETH" : "BTC")}",
                    Group = string.IsNullOrEmpty(wallet.Group) ? "未分组 / Ungrouped" : wallet.Group,
                    Tags = string.IsNullOrEmpty(wallet.Tags) ? "无 / None" : wallet.Tags
                });
            }
        }

        /// <summary>
        /// 组过滤器改变事件
        /// Group filter changed event
        /// </summary>
        private void OnGroupFilterChanged(object sender, EventArgs e)
        {
            if (GroupFilterPicker.SelectedItem == null)
                return;

            var selectedGroup = GroupFilterPicker.SelectedItem.ToString();
            if (selectedGroup == "全部 / All")
            {
                LoadWallets();
            }
            else
            {
                LoadWalletsByGroup(selectedGroup);
            }
        }

        /// <summary>
        /// 标签过滤器改变事件
        /// Tag filter changed event
        /// </summary>
        private void OnTagFilterChanged(object sender, EventArgs e)
        {
            if (TagFilterPicker.SelectedItem == null)
                return;

            var selectedTag = TagFilterPicker.SelectedItem.ToString();
            if (selectedTag == "全部 / All")
            {
                LoadWallets();
            }
            else
            {
                LoadWalletsByTag(selectedTag);
            }
        }

        /// <summary>
        /// 搜索按钮点击事件
        /// Search button click event
        /// </summary>
        private async void OnSearchClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchEntry.Text))
            {
                LoadWallets();
                return;
            }

            try
            {
                var searchText = SearchEntry.Text.Trim().ToLower();
                var allWallets = await _walletService.GetWalletsAsync();
                var filteredWallets = allWallets.Where(w => 
                    w.Name.ToLower().Contains(searchText) || 
                    w.Address.ToLower().Contains(searchText) ||
                    (w.Group != null && w.Group.ToLower().Contains(searchText)) ||
                    (w.Tags != null && w.Tags.ToLower().Contains(searchText)));

                await UpdateWalletList(filteredWallets);
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误 / Error", 
                    $"搜索钱包失败 / Failed to search wallets: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 刷新按钮点击事件
        /// Refresh button click event
        /// </summary>
        private void OnRefreshClicked(object sender, EventArgs e)
        {
            LoadFilters();
            LoadWallets();
        }

        /// <summary>
        /// 查看详情按钮点击事件
        /// View details button click event
        /// </summary>
        private async void OnViewDetailsClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string address)
            {
                var navigationParameter = new Dictionary<string, object>
                {
                    { "address", address }
                };

                await Shell.Current.GoToAsync("WalletDetailsPage", navigationParameter);
            }
        }

        /// <summary>
        /// 复制地址按钮点击事件
        /// Copy address button click event
        /// </summary>
        private async void OnCopyAddressClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string address)
            {
                await Clipboard.SetTextAsync(address);
                await DisplayAlert("成功 / Success", 
                    "地址已复制到剪贴板 / Address copied to clipboard", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 添加钱包按钮点击事件
        /// Add wallet button click event
        /// </summary>
        private async void OnAddWalletClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("CreateWalletPage");
        }
    }

    /// <summary>
    /// 钱包视图模型
    /// Wallet view model
    /// </summary>
    public class WalletViewModel
    {
        /// <summary>
        /// 钱包名称
        /// Wallet name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 钱包地址
        /// Wallet address
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 链类型
        /// Chain type
        /// </summary>
        public string ChainType { get; set; }

        /// <summary>
        /// 余额
        /// Balance
        /// </summary>
        public string Balance { get; set; }

        /// <summary>
        /// 组
        /// Group
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// 标签
        /// Tags
        /// </summary>
        public string Tags { get; set; }
    }
}
