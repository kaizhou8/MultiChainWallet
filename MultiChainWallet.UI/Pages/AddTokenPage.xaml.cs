using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using MultiChainWallet.Core.Models;
using MultiChainWallet.Core.Interfaces;

namespace MultiChainWallet.UI.Pages
{
    public partial class AddTokenPage : ContentPage
    {
        private readonly IERC20Service _erc20Service;
        private readonly string _currentWalletAddress;
        private string _lastValidatedAddress = string.Empty;
        private bool _isValidToken;

        public AddTokenPage(IERC20Service erc20Service, string walletAddress)
        {
            InitializeComponent();
            _erc20Service = erc20Service;
            _currentWalletAddress = walletAddress;
        }

        private async void ContractAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            string address = ContractAddressEntry.Text?.Trim();
            if (string.IsNullOrEmpty(address) || address == _lastValidatedAddress)
                return;

            _lastValidatedAddress = address;

            try
            {
                StatusMessage.Text = "Validating token...";
                TokenInfoPanel.IsVisible = false;
                AddTokenButton.IsEnabled = false;
                _isValidToken = false;

                var tokenInfo = await _erc20Service.GetTokenInfo(address);
                if (tokenInfo != null)
                {
                    TokenNameText.Text = tokenInfo.Name;
                    TokenSymbolText.Text = tokenInfo.Symbol;
                    TokenDecimalsText.Text = tokenInfo.Decimals.ToString();
                    TokenInfoPanel.IsVisible = true;
                    StatusMessage.Text = "Valid token contract found.";
                    AddTokenButton.IsEnabled = true;
                    _isValidToken = true;
                }
                else
                {
                    StatusMessage.Text = "Invalid token contract address.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Error validating token: {ex.Message}";
            }
        }

        private async void AddToken_Click(object sender, EventArgs e)
        {
            if (!_isValidToken)
                return;

            try
            {
                string address = ContractAddressEntry.Text?.Trim();
                bool success = await _erc20Service.AddToken(address);
                if (success)
                {
                    await DisplayAlert("Success", "Token added successfully!", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Error", "Failed to add token.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error adding token: {ex.Message}", "OK");
            }
        }

        private async void Cancel_Click(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
