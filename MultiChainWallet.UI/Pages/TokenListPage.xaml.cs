using System;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using MultiChainWallet.Core.Models;
using MultiChainWallet.Core.Interfaces;

namespace MultiChainWallet.UI.Pages
{
    /// <summary>
    /// 代币列表页面
    /// Token list page
    /// </summary>
    public partial class TokenListPage : ContentPage
    {
        private readonly IERC20Service _erc20Service;
        private readonly string _currentWalletAddress;
        private ObservableCollection<ERC20TokenInfo> _tokens;

        public TokenListPage(IERC20Service erc20Service, string walletAddress)
        {
            InitializeComponent();
            _erc20Service = erc20Service;
            _currentWalletAddress = walletAddress;
            _tokens = new ObservableCollection<ERC20TokenInfo>();
            BindingContext = this;
            LoadTokens();
        }

        private async void LoadTokens()
        {
            try
            {
                var tokens = await _erc20Service.GetUserTokens(_currentWalletAddress);
                _tokens.Clear();
                foreach (var token in tokens)
                {
                    _tokens.Add(token);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error loading tokens: {ex.Message}", "OK");
            }
        }

        private void AddToken_Click(object sender, EventArgs e)
        {
            var addTokenPage = new AddTokenPage(_erc20Service, _currentWalletAddress);
            Navigation.PushAsync(addTokenPage);
        }

        private void SendToken_Click(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var token = (ERC20TokenInfo)button.BindingContext;
            var sendTokenPage = new SendTokenPage(_erc20Service, _currentWalletAddress, token);
            Navigation.PushAsync(sendTokenPage);
        }

        private void ViewDetails_Click(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var token = (ERC20TokenInfo)button.BindingContext;
            var tokenDetailsPage = new TokenDetailsPage(_erc20Service, _currentWalletAddress, token);
            Navigation.PushAsync(tokenDetailsPage);
        }

        private void TokenListView_SelectionChanged(object sender, EventArgs e)
        {
            if (TokenListView.SelectedItem is ERC20TokenInfo token)
            {
                var tokenDetailsPage = new TokenDetailsPage(_erc20Service, _currentWalletAddress, token);
                Navigation.PushAsync(tokenDetailsPage);
            }
        }
    }
}
