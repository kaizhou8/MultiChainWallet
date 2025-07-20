using MultiChainWallet.Core.Models;
using MultiChainWallet.Core.Services;
using MultiChainWallet.Core.Interfaces;
using MultiChainWallet.Infrastructure.Services;
using NBitcoin;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;

namespace MultiChainWallet.UI.Pages
{
    /// <summary>
    /// 发送交易页面
    /// Send transaction page
    /// </summary>
    public partial class SendTransactionPage : ContentPage
    {
        private readonly IWalletService _walletService;
        private readonly IBiometricService _biometricService;
        private readonly BitcoinAddressValidator _bitcoinValidator;
        private readonly EthereumAddressValidator _ethereumValidator;
        private ObservableCollection<WalletAccount> _wallets;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="walletService">钱包服务 / Wallet service</param>
        /// <param name="biometricService">生物识别服务 / Biometric service</param>
        public SendTransactionPage(IWalletService walletService, IBiometricService biometricService)
        {
            InitializeComponent();
            _walletService = walletService;
            _biometricService = biometricService;
            _bitcoinValidator = new BitcoinAddressValidator(Network.TestNet);
            _ethereumValidator = new EthereumAddressValidator();
            LoadWallets();

            // 添加地址输入验证
            // Add address input validation
            ReceiverAddressEntry.TextChanged += OnReceiverAddressChanged;
        }

        /// <summary>
        /// 接收地址变更事件
        /// Receiver address changed event
        /// </summary>
        private async void OnReceiverAddressChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.NewTextValue))
            {
                ReceiverAddressEntry.TextColor = Colors.Black;
                return;
            }

            if (WalletPicker.SelectedItem is not WalletAccount selectedWallet)
            {
                await DisplayAlert("警告 / Warning", 
                    "请先选择发送钱包 / Please select sending wallet first", 
                    "确定 / OK");
                return;
            }

            AddressValidationResult validationResult;
            string warningMessage = null;

            // 根据选择的钱包类型验证地址
            // Validate address based on selected wallet type
            switch (selectedWallet.ChainType)
            {
                case ChainType.Bitcoin:
                    validationResult = await _bitcoinValidator.ValidateAddress(e.NewTextValue);
                    if (validationResult.IsValid && validationResult.Network != "TestNet")
                    {
                        warningMessage = "请使用测试网地址 / Please use testnet address";
                    }
                    break;

                case ChainType.Ethereum:
                    validationResult = await _ethereumValidator.ValidateAddress(e.NewTextValue);
                    if (validationResult.IsValid && validationResult.AddressType == "Contract")
                    {
                        warningMessage = "不建议直接向合约地址发送ETH / Not recommended to send ETH directly to contract address";
                    }
                    break;

                default:
                    validationResult = new AddressValidationResult { IsValid = false, ErrorMessage = "不支持的链类型 / Unsupported chain type" };
                    break;
            }

            // 更新UI
            // Update UI
            if (!validationResult.IsValid)
            {
                ReceiverAddressEntry.TextColor = Colors.Red;
                await DisplayAlert("警告 / Warning", 
                    validationResult.ErrorMessage, 
                    "确定 / OK");
            }
            else
            {
                ReceiverAddressEntry.TextColor = Colors.Green;
                if (!string.IsNullOrEmpty(warningMessage))
                {
                    await DisplayAlert("警告 / Warning", 
                        warningMessage, 
                        "确定 / OK");
                    ReceiverAddressEntry.TextColor = Colors.Orange;
                }
            }
        }

        /// <summary>
        /// 加载钱包列表
        /// Load wallet list
        /// </summary>
        private async void LoadWallets()
        {
            try
            {
                var wallets = await _walletService.GetWalletsAsync();
                _wallets = new ObservableCollection<WalletAccount>(wallets);
                WalletPicker.ItemsSource = _wallets;
                WalletPicker.ItemDisplayBinding = new Binding("Name");

                // 添加钱包选择变更事件
                // Add wallet selection changed event
                WalletPicker.SelectedIndexChanged += (s, e) => 
                {
                    if (!string.IsNullOrWhiteSpace(ReceiverAddressEntry.Text))
                    {
                        OnReceiverAddressChanged(ReceiverAddressEntry, 
                            new TextChangedEventArgs(ReceiverAddressEntry.Text, ReceiverAddressEntry.Text));
                    }
                };
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误 / Error", 
                    $"加载钱包列表失败 / Failed to load wallet list: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 发送交易按钮点击事件
        /// Send transaction button click event
        /// </summary>
        private async void OnSendButtonClicked(object sender, EventArgs e)
        {
            // 验证输入
            // Validate input
            if (string.IsNullOrEmpty(RecipientAddressEntry.Text))
            {
                await DisplayAlert("错误 / Error", "请输入接收地址 / Please enter recipient address", "确定 / OK");
                return;
            }

            if (!decimal.TryParse(AmountEntry.Text, out decimal amount) || amount <= 0)
            {
                await DisplayAlert("错误 / Error", "请输入有效金额 / Please enter a valid amount", "确定 / OK");
                return;
            }

            // 确认交易
            // Confirm transaction
            bool confirmed = await DisplayAlert("确认 / Confirm", 
                $"您确定要发送 {amount} {CoinTypePicker.SelectedItem} 到 {RecipientAddressEntry.Text} 吗? / Are you sure you want to send {amount} {CoinTypePicker.SelectedItem} to {RecipientAddressEntry.Text}?", 
                "确定 / Yes", "取消 / No");

            if (!confirmed)
                return;

            // 检查生物识别是否启用
            // Check if biometrics is enabled
            bool isBiometricsEnabled = Preferences.Default.Get("biometrics_enabled", false);
            bool isTransferVerificationEnabled = Preferences.Default.Get("biometrics_transfer_verification", false);

            if (isBiometricsEnabled && isTransferVerificationEnabled)
            {
                // 进行生物识别验证
                // Perform biometric authentication
                try
                {
                    string transactionInfo = $"{amount} {CoinTypePicker.SelectedItem} -> {RecipientAddressEntry.Text}";
                    var result = await _biometricService.AuthenticateForTransactionAsync(transactionInfo);

                    if (!result.Authenticated)
                    {
                        await DisplayAlert("验证失败 / Authentication Failed", 
                            "生物识别验证失败，交易已取消 / Biometric authentication failed, transaction cancelled", 
                            "确定 / OK");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("错误 / Error", 
                        $"生物识别验证过程中出错: {ex.Message} / Error during biometric authentication: {ex.Message}", 
                        "确定 / OK");
                    return;
                }
            }

            // 执行交易
            // Execute transaction
            try
            {
                // 显示加载指示器
                // Show loading indicator
                LoadingIndicator.IsRunning = true;
                SendButton.IsEnabled = false;

                // 发送交易
                // Send transaction
                string transactionId = await _walletService.SendTransactionAsync(
                    WalletPicker.SelectedItem.ToString(),
                    RecipientAddressEntry.Text,
                    amount,
                    GasLimitEntry.Text,
                    GasPriceEntry.Text);

                // 隐藏加载指示器
                // Hide loading indicator
                LoadingIndicator.IsRunning = false;
                SendButton.IsEnabled = true;

                // 显示成功消息
                // Show success message
                await DisplayAlert("交易成功 / Transaction Successful", 
                    $"交易ID: {transactionId} / Transaction ID: {transactionId}", 
                    "确定 / OK");

                // 重置表单
                // Reset form
                ResetForm();
            }
            catch (Exception ex)
            {
                // 隐藏加载指示器
                // Hide loading indicator
                LoadingIndicator.IsRunning = false;
                SendButton.IsEnabled = true;

                // 显示错误消息
                // Show error message
                await DisplayAlert("交易失败 / Transaction Failed", 
                    $"错误: {ex.Message} / Error: {ex.Message}", 
                    "确定 / OK");
            }
        }
    }
}
