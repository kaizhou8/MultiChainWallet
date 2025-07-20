using MultiChainWallet.Core.Models;
using MultiChainWallet.Core.Services;
using System.Collections.ObjectModel;

namespace MultiChainWallet.UI.Pages
{
    /// <summary>
    /// 钱包组管理页面
    /// Wallet groups management page
    /// </summary>
    public partial class WalletGroupsPage : ContentPage
    {
        private readonly IWalletService _walletService;
        private ObservableCollection<GroupViewModel> _groups;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="walletService">钱包服务 / Wallet service</param>
        public WalletGroupsPage(IWalletService walletService)
        {
            InitializeComponent();
            _walletService = walletService;
            _groups = new ObservableCollection<GroupViewModel>();
            GroupsCollection.ItemsSource = _groups;
            LoadGroups();
        }

        /// <summary>
        /// 加载钱包组
        /// Load wallet groups
        /// </summary>
        private async void LoadGroups()
        {
            try
            {
                var groups = await _walletService.GetAllGroupsAsync();
                _groups.Clear();

                foreach (var group in groups)
                {
                    var wallets = await _walletService.GetWalletsByGroupAsync(group);
                    _groups.Add(new GroupViewModel
                    {
                        Name = group,
                        WalletCount = wallets.Count()
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误 / Error", 
                    $"加载钱包组失败 / Failed to load wallet groups: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 添加组按钮点击事件
        /// Add group button click event
        /// </summary>
        private async void OnAddGroupClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GroupNameEntry.Text))
            {
                await DisplayAlert("提示 / Notice", 
                    "请输入组名 / Please enter a group name", 
                    "确定 / OK");
                return;
            }

            var result = await DisplayAlert("确认 / Confirm", 
                $"是否创建组 '{GroupNameEntry.Text}'? / Create group '{GroupNameEntry.Text}'?", 
                "是 / Yes", "否 / No");

            if (result)
            {
                // 这里我们只是创建了一个空组，用户需要在钱包详情页面将钱包添加到组中
                // Here we just create an empty group, users need to add wallets to the group in the wallet details page
                _groups.Add(new GroupViewModel
                {
                    Name = GroupNameEntry.Text,
                    WalletCount = 0
                });

                GroupNameEntry.Text = string.Empty;
            }
        }

        /// <summary>
        /// 查看组按钮点击事件
        /// View group button click event
        /// </summary>
        private async void OnViewGroupClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string groupName)
            {
                var navigationParameter = new Dictionary<string, object>
                {
                    { "GroupName", groupName }
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
            LoadGroups();
        }
    }

    /// <summary>
    /// 组视图模型
    /// Group view model
    /// </summary>
    public class GroupViewModel
    {
        /// <summary>
        /// 组名
        /// Group name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 钱包数量
        /// Wallet count
        /// </summary>
        public int WalletCount { get; set; }
    }
}
