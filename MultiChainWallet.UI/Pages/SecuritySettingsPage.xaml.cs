using Microsoft.Extensions.Logging;
using MultiChainWallet.UI.ViewModels;

namespace MultiChainWallet.UI.Pages
{
    public partial class SecuritySettingsPage : ContentPage
    {
        private readonly SecuritySettingsViewModel _viewModel;
        private readonly ILogger<SecuritySettingsPage> _logger;

        public SecuritySettingsPage(
            SecuritySettingsViewModel viewModel,
            ILogger<SecuritySettingsPage> logger)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _logger = logger;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                await _viewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化安全设置页面时出错 / Error initializing security settings page");
                await DisplayAlert(
                    "错误 / Error",
                    "加载安全设置时出错。请稍后重试。/ Error loading security settings. Please try again later.",
                    "确定 / OK");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.Cleanup();
        }
    }
} 