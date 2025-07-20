using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace MultiChainWallet.UI.Pages
{
    /// <summary>
    /// 交易历史页面
    /// Transaction history page
    /// </summary>
    public partial class TransactionHistoryPage : ContentPage
    {
        private bool isRefreshing;
        public bool IsRefreshing
        {
            get => isRefreshing;
            set
            {
                isRefreshing = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TransactionViewModel> Transactions { get; }
        public ICommand RefreshCommand { get; }

        public TransactionHistoryPage()
        {
            InitializeComponent();
            BindingContext = this;

            Transactions = new ObservableCollection<TransactionViewModel>();
            RefreshCommand = new Command(async () => await RefreshTransactions());

            LoadWallets();
        }

        /// <summary>
        /// 加载钱包列表
        /// Load wallet list
        /// </summary>
        private void LoadWallets()
        {
            // TODO: Load wallets from wallet service
            var wallets = new List<string> { "所有钱包 / All Wallets" };
            WalletPicker.ItemsSource = wallets;
            WalletPicker.SelectedIndex = 0;
        }

        /// <summary>
        /// 刷新交易记录
        /// Refresh transactions
        /// </summary>
        private async Task RefreshTransactions()
        {
            try
            {
                IsRefreshing = true;

                // Clear existing transactions
                Transactions.Clear();

                // Get selected wallet and type filters
                string selectedWallet = WalletPicker.SelectedItem?.ToString();
                string selectedType = TypePicker.SelectedItem?.ToString();

                // TODO: Get transactions from service
                var transactions = await GetTransactions(selectedWallet, selectedType);

                // Update UI
                foreach (var transaction in transactions)
                {
                    Transactions.Add(transaction);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误 / Error", 
                    $"加载交易记录失败 / Failed to load transactions: {ex.Message}", 
                    "确定 / OK");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        /// <summary>
        /// 获取交易记录
        /// Get transactions
        /// </summary>
        private async Task<List<TransactionViewModel>> GetTransactions(string wallet, string type)
        {
            // TODO: Implement actual transaction fetching logic
            return new List<TransactionViewModel>
            {
                new TransactionViewModel
                {
                    Type = "发送 / Send",
                    Address = "0x1234...5678",
                    DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Amount = "-0.1 ETH",
                    Status = "已确认 / Confirmed",
                    StatusColor = Colors.Green,
                    TypeIcon = "send_icon.png"
                }
            };
        }

        /// <summary>
        /// 钱包选择改变事件
        /// Wallet selection changed event
        /// </summary>
        private async void OnWalletSelected(object sender, EventArgs e)
        {
            await RefreshTransactions();
        }

        /// <summary>
        /// 交易类型选择改变事件
        /// Transaction type selection changed event
        /// </summary>
        private async void OnTypeSelected(object sender, EventArgs e)
        {
            await RefreshTransactions();
        }

        /// <summary>
        /// 刷新按钮点击事件
        /// Refresh button clicked event
        /// </summary>
        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await RefreshTransactions();
        }

        /// <summary>
        /// 交易记录选择事件
        /// Transaction selection event
        /// </summary>
        private async void OnTransactionSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is TransactionViewModel transaction)
            {
                // Navigate to transaction details page
                var parameters = new Dictionary<string, object>
                {
                    { "transaction", transaction }
                };
                await Shell.Current.GoToAsync("TransactionDetailsPage", parameters);
                
                // Clear selection
                ((CollectionView)sender).SelectedItem = null;
            }
        }
    }

    /// <summary>
    /// 交易视图模型
    /// Transaction view model
    /// </summary>
    public class TransactionViewModel
    {
        public string Type { get; set; }
        public string Address { get; set; }
        public string DateTime { get; set; }
        public string Amount { get; set; }
        public string Status { get; set; }
        public Color StatusColor { get; set; }
        public string TypeIcon { get; set; }
    }
}
