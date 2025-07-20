using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using MultiChainWallet.Core.Models.MarketData;
using MultiChainWallet.Core.Services.MarketData;
using System.Linq;
using System.Globalization;

namespace MultiChainWallet.UI.Pages
{
    [QueryProperty(nameof(CoinId), "CoinId")]
    public partial class CryptoDetailsPage : ContentPage
    {
        private readonly IMarketDataService _marketDataService;
        private CryptoDetailsViewModel _viewModel;

        private string _coinId;
        public string CoinId
        {
            get => _coinId;
            set
            {
                _coinId = value;
                LoadCryptoDetailsAsync(_coinId);
            }
        }

        public CryptoDetailsPage(IMarketDataService marketDataService)
        {
            InitializeComponent();
            _marketDataService = marketDataService;
            _viewModel = new CryptoDetailsViewModel(marketDataService);
            BindingContext = _viewModel;
        }

        private async void LoadCryptoDetailsAsync(string coinId)
        {
            if (string.IsNullOrEmpty(coinId))
                return;

            await _viewModel.LoadCryptoDetailsAsync(coinId);
        }
    }

    public class CryptoDetailsViewModel : BindableObject
    {
        private readonly IMarketDataService _marketDataService;
        
        private string _id;
        private string _name;
        private string _symbol;
        private string _imageUrl;
        private decimal _currentPrice;
        private decimal _priceChange24h;
        private decimal _priceChangePercentage24h;
        private Color _priceChangeColor;
        private decimal _marketCap;
        private decimal _volume24h;
        private decimal _high24h;
        private decimal _low24h;
        private decimal _allTimeHigh;
        private decimal _allTimeLow;
        private DateTime _lastUpdated;
        private string _description;
        private decimal _circulatingSupply;
        private decimal _totalSupply;
        private decimal _maxSupply;
        private string _blockchain;
        private double _priceRangeWidth;
        private bool _isLoading;
        private bool _isRefreshing;
        private bool _hasError;
        private string _errorMessage;
        private bool _isDataLoaded;
        private bool _hasDescription;
        private bool _hasLinks;
        private bool _hasWebsite;
        private bool _hasWhitepaper;
        private bool _hasGithub;
        private bool _hasTwitter;
        private string _websiteUrl;
        private string _whitepaperUrl;
        private string _githubUrl;
        private string _twitterUrl;

        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Symbol
        {
            get => _symbol;
            set
            {
                _symbol = value;
                OnPropertyChanged();
            }
        }

        public string ImageUrl
        {
            get => _imageUrl;
            set
            {
                _imageUrl = value;
                OnPropertyChanged();
            }
        }

        public decimal CurrentPrice
        {
            get => _currentPrice;
            set
            {
                _currentPrice = value;
                OnPropertyChanged();
            }
        }

        public decimal PriceChange24h
        {
            get => _priceChange24h;
            set
            {
                _priceChange24h = value;
                OnPropertyChanged();
            }
        }

        public decimal PriceChangePercentage24h
        {
            get => _priceChangePercentage24h;
            set
            {
                _priceChangePercentage24h = value;
                OnPropertyChanged();
                PriceChangeColor = value >= 0 ? Colors.Green : Colors.Red;
            }
        }

        public Color PriceChangeColor
        {
            get => _priceChangeColor;
            set
            {
                _priceChangeColor = value;
                OnPropertyChanged();
            }
        }

        public decimal MarketCap
        {
            get => _marketCap;
            set
            {
                _marketCap = value;
                OnPropertyChanged();
            }
        }

        public decimal Volume24h
        {
            get => _volume24h;
            set
            {
                _volume24h = value;
                OnPropertyChanged();
            }
        }

        public decimal High24h
        {
            get => _high24h;
            set
            {
                _high24h = value;
                OnPropertyChanged();
                UpdatePriceRangeWidth();
            }
        }

        public decimal Low24h
        {
            get => _low24h;
            set
            {
                _low24h = value;
                OnPropertyChanged();
                UpdatePriceRangeWidth();
            }
        }

        public decimal AllTimeHigh
        {
            get => _allTimeHigh;
            set
            {
                _allTimeHigh = value;
                OnPropertyChanged();
            }
        }

        public decimal AllTimeLow
        {
            get => _allTimeLow;
            set
            {
                _allTimeLow = value;
                OnPropertyChanged();
            }
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set
            {
                _lastUpdated = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
                HasDescription = !string.IsNullOrEmpty(value);
            }
        }

        public decimal CirculatingSupply
        {
            get => _circulatingSupply;
            set
            {
                _circulatingSupply = value;
                OnPropertyChanged();
            }
        }

        public decimal TotalSupply
        {
            get => _totalSupply;
            set
            {
                _totalSupply = value;
                OnPropertyChanged();
            }
        }

        public decimal MaxSupply
        {
            get => _maxSupply;
            set
            {
                _maxSupply = value;
                OnPropertyChanged();
            }
        }

        public string Blockchain
        {
            get => _blockchain;
            set
            {
                _blockchain = value;
                OnPropertyChanged();
            }
        }

        public double PriceRangeWidth
        {
            get => _priceRangeWidth;
            set
            {
                _priceRangeWidth = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                IsDataLoaded = !value && !HasError;
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

        public bool HasError
        {
            get => _hasError;
            set
            {
                _hasError = value;
                OnPropertyChanged();
                IsDataLoaded = !IsLoading && !value;
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsDataLoaded
        {
            get => _isDataLoaded;
            set
            {
                _isDataLoaded = value;
                OnPropertyChanged();
            }
        }

        public bool HasDescription
        {
            get => _hasDescription;
            set
            {
                _hasDescription = value;
                OnPropertyChanged();
            }
        }

        public bool HasLinks
        {
            get => _hasLinks;
            set
            {
                _hasLinks = value;
                OnPropertyChanged();
            }
        }

        public bool HasWebsite
        {
            get => _hasWebsite;
            set
            {
                _hasWebsite = value;
                OnPropertyChanged();
            }
        }

        public bool HasWhitepaper
        {
            get => _hasWhitepaper;
            set
            {
                _hasWhitepaper = value;
                OnPropertyChanged();
            }
        }

        public bool HasGithub
        {
            get => _hasGithub;
            set
            {
                _hasGithub = value;
                OnPropertyChanged();
            }
        }

        public bool HasTwitter
        {
            get => _hasTwitter;
            set
            {
                _hasTwitter = value;
                OnPropertyChanged();
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand OpenWebsiteCommand { get; }
        public ICommand OpenWhitepaperCommand { get; }
        public ICommand OpenGithubCommand { get; }
        public ICommand OpenTwitterCommand { get; }

        public CryptoDetailsViewModel(IMarketDataService marketDataService)
        {
            _marketDataService = marketDataService ?? throw new ArgumentNullException(nameof(marketDataService));
            
            RefreshCommand = new Command(async () => await RefreshDataAsync());
            OpenWebsiteCommand = new Command(OpenWebsite);
            OpenWhitepaperCommand = new Command(OpenWhitepaper);
            OpenGithubCommand = new Command(OpenGithub);
            OpenTwitterCommand = new Command(OpenTwitter);

            // 默认值
            // Default values
            PriceChangeColor = Colors.Green;
            IsLoading = true;
            HasError = false;
            IsDataLoaded = false;
            HasDescription = false;
            HasLinks = false;
            HasWebsite = false;
            HasWhitepaper = false;
            HasGithub = false;
            HasTwitter = false;
        }

        public async Task LoadCryptoDetailsAsync(string coinId)
        {
            if (string.IsNullOrEmpty(coinId))
                return;

            Id = coinId;
            IsLoading = true;
            HasError = false;
            IsRefreshing = false;

            try
            {
                // 获取币种市场数据
                // Get coin market data
                var marketInfo = await _marketDataService.GetMarketInfoAsync(coinId, true);
                
                if (marketInfo != null)
                {
                    Name = marketInfo.Name;
                    Symbol = marketInfo.Symbol.ToUpper();
                    ImageUrl = marketInfo.ImageUrl;
                    CurrentPrice = marketInfo.CurrentPrice;
                    PriceChange24h = marketInfo.PriceChange24h;
                    PriceChangePercentage24h = marketInfo.PriceChangePercentage24h;
                    MarketCap = marketInfo.MarketCap;
                    Volume24h = marketInfo.TotalVolume;
                    High24h = marketInfo.High24h;
                    Low24h = marketInfo.Low24h;
                    AllTimeHigh = marketInfo.Ath;
                    AllTimeLow = marketInfo.Atl;
                    LastUpdated = marketInfo.LastUpdated;
                    Description = marketInfo.Description != null && marketInfo.Description.ContainsKey("en") ? 
                        marketInfo.Description["en"] : string.Empty;
                    CirculatingSupply = marketInfo.CirculatingSupply;
                    TotalSupply = marketInfo.TotalSupply;
                    MaxSupply = marketInfo.MaxSupply;
                    Blockchain = DetermineBlocchain(marketInfo);

                    // 设置链接
                    // Set links
                    SetLinksFromMarketInfo(marketInfo);
                    
                    // 更新价格范围滑块
                    // Update price range slider
                    UpdatePriceRangeWidth();
                }
                else
                {
                    SetErrorState("无法获取数据。请稍后再试。 / Could not fetch data. Please try again later.");
                }
            }
            catch (Exception ex)
            {
                SetErrorState($"加载数据时出错: {ex.Message} / Error loading data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
            }
        }

        private async Task RefreshDataAsync()
        {
            if (string.IsNullOrEmpty(Id))
                return;

            IsRefreshing = true;
            await LoadCryptoDetailsAsync(Id);
        }

        private void UpdatePriceRangeWidth()
        {
            if (High24h == 0 || Low24h == 0 || CurrentPrice == 0)
            {
                PriceRangeWidth = 50; // 默认值 / Default value
                return;
            }

            // 计算当前价格在24小时范围中的位置
            // Calculate current price position within 24h range
            var range = High24h - Low24h;
            if (range <= 0)
            {
                PriceRangeWidth = 50;
                return;
            }
            
            var position = (CurrentPrice - Low24h) / range;
            PriceRangeWidth = (double)(position * 100);
        }

        private string DetermineBlocchain(CryptoMarketInfo marketInfo)
        {
            // 根据币种ID或类别确定区块链
            // Determine blockchain based on coin ID or categories
            var id = marketInfo.Id.ToLower();
            
            if (id.Contains("bitcoin") || id == "btc")
                return "Bitcoin";
            
            if (id.Contains("ethereum") || id == "eth")
                return "Ethereum";
            
            if (id.Contains("binance") || id == "bnb")
                return "Binance Smart Chain";
            
            if (id.Contains("cardano") || id == "ada")
                return "Cardano";
            
            if (id.Contains("solana") || id == "sol")
                return "Solana";
            
            if (id.Contains("polkadot") || id == "dot")
                return "Polkadot";
            
            if (id.Contains("ripple") || id == "xrp")
                return "XRP Ledger";
            
            if (marketInfo.Categories != null && marketInfo.Categories.Any())
            {
                foreach (var category in marketInfo.Categories)
                {
                    if (category.ToLower().Contains("token"))
                        return "Ethereum (ERC20)";
                }
            }
            
            return "Other";
        }

        private void SetLinksFromMarketInfo(CryptoMarketInfo marketInfo)
        {
            if (marketInfo.Links == null)
            {
                HasLinks = false;
                return;
            }

            // 设置网站链接
            // Set website link
            if (marketInfo.Links.Homepage != null && marketInfo.Links.Homepage.Any() && 
                !string.IsNullOrEmpty(marketInfo.Links.Homepage.First()))
            {
                HasWebsite = true;
                _websiteUrl = marketInfo.Links.Homepage.First();
            }
            else
            {
                HasWebsite = false;
            }

            // 设置白皮书链接
            // Set whitepaper link
            if (marketInfo.Links.Whitepaper != null && !string.IsNullOrEmpty(marketInfo.Links.Whitepaper))
            {
                HasWhitepaper = true;
                _whitepaperUrl = marketInfo.Links.Whitepaper;
            }
            else
            {
                HasWhitepaper = false;
            }

            // 设置GitHub链接
            // Set GitHub link
            if (marketInfo.Links.ReposUrl != null && 
                marketInfo.Links.ReposUrl.ContainsKey("github") && 
                marketInfo.Links.ReposUrl["github"] != null && 
                marketInfo.Links.ReposUrl["github"].Any())
            {
                HasGithub = true;
                _githubUrl = marketInfo.Links.ReposUrl["github"].First();
            }
            else
            {
                HasGithub = false;
            }

            // 设置Twitter链接
            // Set Twitter link
            if (marketInfo.Links.TwitterScreenName != null && !string.IsNullOrEmpty(marketInfo.Links.TwitterScreenName))
            {
                HasTwitter = true;
                _twitterUrl = $"https://twitter.com/{marketInfo.Links.TwitterScreenName}";
            }
            else
            {
                HasTwitter = false;
            }

            // 更新是否有链接
            // Update has links
            HasLinks = HasWebsite || HasWhitepaper || HasGithub || HasTwitter;
        }

        private void SetErrorState(string message)
        {
            HasError = true;
            ErrorMessage = message;
            IsDataLoaded = false;
        }

        private async void OpenWebsite()
        {
            if (!string.IsNullOrEmpty(_websiteUrl))
            {
                await Browser.OpenAsync(_websiteUrl, BrowserLaunchMode.SystemPreferred);
            }
        }

        private async void OpenWhitepaper()
        {
            if (!string.IsNullOrEmpty(_whitepaperUrl))
            {
                await Browser.OpenAsync(_whitepaperUrl, BrowserLaunchMode.SystemPreferred);
            }
        }

        private async void OpenGithub()
        {
            if (!string.IsNullOrEmpty(_githubUrl))
            {
                await Browser.OpenAsync(_githubUrl, BrowserLaunchMode.SystemPreferred);
            }
        }

        private async void OpenTwitter()
        {
            if (!string.IsNullOrEmpty(_twitterUrl))
            {
                await Browser.OpenAsync(_twitterUrl, BrowserLaunchMode.SystemPreferred);
            }
        }
    }
} 