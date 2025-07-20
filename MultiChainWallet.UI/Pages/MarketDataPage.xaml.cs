using System;
using Microsoft.Maui.Controls;
using MultiChainWallet.Core.Models.MarketData;
using MultiChainWallet.Core.Services.MarketData;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;
using System.Threading.Tasks;

namespace MultiChainWallet.UI.Pages
{
    public partial class MarketDataPage : ContentPage
    {
        private readonly MarketDataViewModel _viewModel;
        private readonly IMarketDataService _marketDataService;

        public MarketDataPage(IMarketDataService marketDataService)
        {
            InitializeComponent();
            _marketDataService = marketDataService;
            _viewModel = new MarketDataViewModel(_marketDataService);
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadCryptoPricesAsync();
        }

        private async void OnCryptoSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is CryptoPriceDisplayModel selectedCrypto)
            {
                // 清除选择状态
                // Clear selection
                ((CollectionView)sender).SelectedItem = null;

                // 导航到详情页面
                // Navigate to details page
                var parameters = new Dictionary<string, object>
                {
                    { "CoinId", selectedCrypto.Id }
                };
                await Shell.Current.GoToAsync("CryptoDetailsPage", parameters);
            }
        }
    }

    public class MarketDataViewModel : BindableObject
    {
        private readonly IMarketDataService _marketDataService;
        private ObservableCollection<CryptoPriceDisplayModel> _cryptoPrices;
        private bool _isRefreshing;
        private string _searchQuery = string.Empty;
        private ObservableCollection<CryptoPriceDisplayModel> _allCryptoPrices;

        public ObservableCollection<CryptoPriceDisplayModel> CryptoPrices
        {
            get => _cryptoPrices;
            set
            {
                _cryptoPrices = value;
                OnPropertyChanged();
            }
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                _isRefreshing = value;
                OnPropertyChanged();
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }

        public MarketDataViewModel(IMarketDataService marketDataService)
        {
            _marketDataService = marketDataService;
            CryptoPrices = new ObservableCollection<CryptoPriceDisplayModel>();
            _allCryptoPrices = new ObservableCollection<CryptoPriceDisplayModel>();
            
            RefreshCommand = new Command(async () => await RefreshDataAsync());
            SearchCommand = new Command<string>(SearchCryptocurrencies);
        }

        public async Task LoadCryptoPricesAsync()
        {
            try
            {
                IsRefreshing = true;
                
                // 获取热门加密货币价格
                // Get popular cryptocurrency prices
                var popularCoinIds = new[] { "bitcoin", "ethereum", "ripple", "cardano", "solana", "binancecoin", "polkadot", "dogecoin" };
                var prices = await _marketDataService.GetCryptoPricesAsync(popularCoinIds);
                
                _allCryptoPrices.Clear();
                foreach (var price in prices)
                {
                    _allCryptoPrices.Add(new CryptoPriceDisplayModel
                    {
                        Id = price.Id,
                        Name = price.Name,
                        Symbol = price.Symbol.ToUpper(),
                        CurrentPrice = price.CurrentPrice,
                        PriceChangePercentage24h = price.PriceChangePercentage24h,
                        PriceChangeColor = price.PriceChangePercentage24h >= 0 ? Colors.Green : Colors.Red,
                        MarketCap = price.MarketCap,
                        Volume24h = price.Volume24h,
                        ImageUrl = price.ImageUrl
                    });
                }
                
                // 应用搜索过滤
                // Apply search filter
                ApplySearchFilter();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "错误 / Error", 
                    $"加载市场数据时出错 / Error loading market data: {ex.Message}", 
                    "确定 / OK");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task RefreshDataAsync()
        {
            await LoadCryptoPricesAsync();
        }

        private void SearchCryptocurrencies(string query)
        {
            _searchQuery = query?.ToLower() ?? string.Empty;
            ApplySearchFilter();
        }

        private void ApplySearchFilter()
        {
            if (string.IsNullOrWhiteSpace(_searchQuery))
            {
                CryptoPrices = new ObservableCollection<CryptoPriceDisplayModel>(_allCryptoPrices);
            }
            else
            {
                var filtered = _allCryptoPrices.Where(c => 
                    c.Name.ToLower().Contains(_searchQuery) || 
                    c.Symbol.ToLower().Contains(_searchQuery));
                
                CryptoPrices = new ObservableCollection<CryptoPriceDisplayModel>(filtered);
            }
        }
    }

    public class CryptoPriceDisplayModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal PriceChangePercentage24h { get; set; }
        public Color PriceChangeColor { get; set; }
        public decimal MarketCap { get; set; }
        public decimal Volume24h { get; set; }
        public string ImageUrl { get; set; }
    }
} 