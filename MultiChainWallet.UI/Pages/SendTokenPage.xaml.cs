using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using MultiChainWallet.Core.Models;
using MultiChainWallet.Core.Interfaces;

namespace MultiChainWallet.UI.Pages
{
    public partial class SendTokenPage : ContentPage
    {
        private readonly IERC20Service _erc20Service;
        private readonly string _currentWalletAddress;
        private readonly ERC20TokenInfo _token;
        private decimal _currentGasPrice;
        private bool _isValidAddress;
        private bool _isValidAmount;

        public SendTokenPage(IERC20Service erc20Service, string walletAddress, ERC20TokenInfo token)
        {
            InitializeComponent();
            _erc20Service = erc20Service;
            _currentWalletAddress = walletAddress;
            _token = token;

            TokenSymbolText.Text = token.Symbol;
            TokenBalanceText.Text = $"{token.Balance:N4} {token.Symbol}";
            TokenSymbolLabel.Text = token.Symbol;
        }

        private async void RecipientAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            string address = RecipientAddressEntry.Text?.Trim();
            _isValidAddress = !string.IsNullOrEmpty(address) && await _erc20Service.IsValidAddress(address);
            UpdateSendButtonState();

            if (_isValidAddress)
            {
                await UpdateGasFeeEstimate();
            }
        }

        private async void Amount_TextChanged(object sender, TextChangedEventArgs e)
        {
            string amountText = AmountEntry.Text?.Trim();
            _isValidAmount = decimal.TryParse(amountText, out decimal amount) && amount > 0 && amount <= _token.Balance;
            UpdateSendButtonState();

            if (_isValidAmount)
            {
                // Update USD value
                decimal usdValue = amount * _token.Value / _token.Balance;
                USDValueText.Text = usdValue.ToString("N2");

                await UpdateGasFeeEstimate();
            }
            else
            {
                USDValueText.Text = "0.00";
            }
        }

        private void UpdateSendButtonState()
        {
            SendButton.IsEnabled = _isValidAddress && _isValidAmount;
        }

        private async Task UpdateGasFeeEstimate()
        {
            try
            {
                string address = RecipientAddressEntry.Text?.Trim();
                if (decimal.TryParse(AmountEntry.Text?.Trim(), out decimal amount) && !string.IsNullOrEmpty(address))
                {
                    _currentGasPrice = await _erc20Service.EstimateGasFee(_token.ContractAddress, address, amount);
                    GasFeeText.Text = $"{_currentGasPrice:N8} ETH (≈ ${_currentGasPrice * 2000:N2})"; // Assuming 1 ETH = $2000
                    GasFeePanel.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error estimating gas fee: {ex.Message}", "OK");
            }
        }

        private async void Send_Click(object sender, EventArgs e)
        {
            if (!_isValidAddress || !_isValidAmount)
                return;

            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;
                SendButton.IsEnabled = false;

                string address = RecipientAddressEntry.Text?.Trim();
                decimal amount = decimal.Parse(AmountEntry.Text?.Trim());
                string password = PasswordEntry.Text;

                bool success = await _erc20Service.SendToken(_token.ContractAddress, address, amount, password);
                if (success)
                {
                    await DisplayAlert("Success", "Transaction sent successfully!", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Error", "Failed to send transaction.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error sending transaction: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
                SendButton.IsEnabled = true;
            }
        }

        private async void Cancel_Click(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
