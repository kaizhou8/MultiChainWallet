using Microsoft.Extensions.Logging;
using MultiChainWallet.UI.ViewModels;

namespace MultiChainWallet.UI.Pages
{
    public partial class TransactionConfirmationPage : ContentPage
    {
        private readonly TransactionConfirmationViewModel _viewModel;
        private readonly ILogger<TransactionConfirmationPage> _logger;

        public TransactionConfirmationPage(
            TransactionConfirmationViewModel viewModel,
            ILogger<TransactionConfirmationPage> logger)
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
                _logger.LogError(ex, "初始化交易确认页面时出错 / Error initializing transaction confirmation page");
                await DisplayAlert(
                    "错误 / Error",
                    "加载交易确认页面时出错。请稍后重试。/ Error loading transaction confirmation page. Please try again later.",
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