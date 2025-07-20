using System;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using MultiChainWallet.Core.Models;
using MultiChainWallet.Core.Interfaces;

namespace MultiChainWallet.UI.Pages
{
    public partial class TokenDetailsPage : ContentPage
    {
        private readonly IERC20Service _erc20Service;
        private readonly string _currentWalletAddress;
        private readonly ERC20TokenInfo _token;
        private ObservableCollection<TokenTransaction> _transactions;

        public ERC20TokenInfo Token => _token;
        public ObservableCollection<TokenTransaction> Transactions => _transactions;

        public TokenDetailsPage(IERC20Service erc20Service, string walletAddress, ERC20TokenInfo token)
        {
            InitializeComponent();
            _erc20Service = erc20Service;
            _currentWalletAddress = walletAddress;
            _token = token;
            _transactions = new ObservableCollection<TokenTransaction>();
            BindingContext = this;
            LoadTransactions();
        }

        private async void LoadTransactions()
        {
            try
            {
                var transactions = await _erc20Service.GetTokenTransactions(_token.ContractAddress, _currentWalletAddress);
                _transactions.Clear();
                foreach (var transaction in transactions)
                {
                    _transactions.Add(transaction);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error loading transactions: {ex.Message}", "OK");
            }
        }

        private async void SendToken_Click(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SendTokenPage(_erc20Service, _currentWalletAddress, _token));
        }

        private async void Back_Click(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    public class TokenTransaction
    {
        public string Type { get; set; }
        public string Address { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Symbol { get; set; }
    }
}
