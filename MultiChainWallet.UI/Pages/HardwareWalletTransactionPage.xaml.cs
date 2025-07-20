using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Models;
using MultiChainWallet.Core.Services;
using MultiChainWallet.Infrastructure.Services;
using Nethereum.Hex.HexConvertors.Extensions;

namespace MultiChainWallet.UI.Pages
{
    /// <summary>
    /// 硬件钱包交易页面
    /// Hardware wallet transaction page
    /// </summary>
    public partial class HardwareWalletTransactionPage : ContentPage
    {
        private readonly HardwareWalletManager _hardwareWalletManager;
        private readonly IWalletService _walletService;
        private readonly ILogger<HardwareWalletTransactionPage> _logger;
        private CoinType _selectedCoinType;
        private byte[] _unsignedTransaction;
        private byte[] _signedTransaction;
        private string _derivationPath;
        private Dictionary<int, string> _inputDerivationPaths;
        private string _transactionId;
        private bool _isConnected = false;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public HardwareWalletTransactionPage(
            HardwareWalletManager hardwareWalletManager,
            IWalletService walletService,
            ILogger<HardwareWalletTransactionPage> logger)
        {
            InitializeComponent();
            _hardwareWalletManager = hardwareWalletManager;
            _walletService = walletService;
            _logger = logger;
        }

        /// <summary>
        /// 页面出现时
        /// When page appears
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            CheckConnectionStatus();
        }

        /// <summary>
        /// 检查连接状态
        /// Check connection status
        /// </summary>
        private async void CheckConnectionStatus()
        {
            try
            {
                var connectedWalletType = _hardwareWalletManager.GetConnectedWalletType();
                if (connectedWalletType.HasValue)
                {
                    _isConnected = true;
                    ConnectionStatusLabel.Text = $"已连接到 {connectedWalletType.Value} / Connected to {connectedWalletType.Value}";
                    ConnectionStatusLabel.TextColor = Colors.Green;
                    ConnectWalletButton.Text = "断开连接 / Disconnect";
                    
                    // 显示交易框架
                    // Show transaction frame
                    Step1Frame_Content.IsVisible = true;
                    
                    // 获取当前地址
                    // Get current address
                    await LoadAddresses();
                }
                else
                {
                    _isConnected = false;
                    ConnectionStatusLabel.Text = "未连接 / Not Connected";
                    ConnectionStatusLabel.TextColor = Colors.Red;
                    ConnectWalletButton.Text = "连接硬件钱包 / Connect Hardware Wallet";
                    
                    // 隐藏交易框架
                    // Hide transaction frame
                    Step1Frame_Content.IsVisible = false;
                    Step2Frame_Content.IsVisible = false;
                    Step3Frame_Content.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查连接状态时出错 / Error checking connection status");
            }
        }

        /// <summary>
        /// 加载地址
        /// Load addresses
        /// </summary>
        private async Task LoadAddresses()
        {
            try
            {
                // 获取以太坊地址
                // Get Ethereum address
                string ethAddress = await _hardwareWalletManager.GetAddressAsync(
                    CoinType.Ethereum, 
                    0, // 账户索引 / Account index
                    0, // 地址索引 / Address index
                    false); // 不在设备上显示 / Don't show on device
                
                EthFromAddressEntry.Text = ethAddress;
                
                // 获取比特币地址
                // Get Bitcoin address
                string btcAddress = await _hardwareWalletManager.GetAddressAsync(
                    CoinType.Bitcoin, 
                    0, // 账户索引 / Account index
                    0, // 地址索引 / Address index
                    false); // 不在设备上显示 / Don't show on device
                
                BtcFromAddressEntry.Text = btcAddress;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载地址时出错 / Error loading addresses");
                await DisplayAlert("错误 / Error", 
                    $"加载地址时出错: {ex.Message} / Error loading addresses: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 连接钱包按钮点击事件
        /// Connect wallet button click event
        /// </summary>
        private async void ConnectWalletButton_Clicked(object sender, EventArgs e)
        {
            if (_isConnected)
            {
                // 断开连接
                // Disconnect
                try
                {
                    await _hardwareWalletManager.DisconnectAsync();
                    _isConnected = false;
                    CheckConnectionStatus();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "断开硬件钱包连接时出错 / Error disconnecting from hardware wallet");
                    await DisplayAlert("错误 / Error", 
                        $"断开硬件钱包连接时出错: {ex.Message} / Error disconnecting from hardware wallet: {ex.Message}", 
                        "确定 / OK");
                }
            }
            else
            {
                // 导航到连接页面
                // Navigate to connection page
                await Navigation.PushAsync(new HardwareWalletConnectionPage(_hardwareWalletManager, _logger));
            }
        }

        /// <summary>
        /// 币种类型选择变更事件
        /// Coin type selection changed event
        /// </summary>
        private void CoinTypePicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedItem = CoinTypePicker.SelectedItem as string;
            if (!string.IsNullOrEmpty(selectedItem))
            {
                if (selectedItem.Contains("以太坊"))
                {
                    _selectedCoinType = CoinType.Ethereum;
                    EthereumTransactionForm.IsVisible = true;
                    BitcoinTransactionForm.IsVisible = false;
                }
                else if (selectedItem.Contains("比特币"))
                {
                    _selectedCoinType = CoinType.Bitcoin;
                    EthereumTransactionForm.IsVisible = false;
                    BitcoinTransactionForm.IsVisible = true;
                }
            }
        }

        /// <summary>
        /// 构建交易按钮点击事件
        /// Build transaction button click event
        /// </summary>
        private async void BuildTransactionButton_Clicked(object sender, EventArgs e)
        {
            if (!_isConnected)
            {
                await DisplayAlert("错误 / Error", 
                    "请先连接硬件钱包 / Please connect to hardware wallet first", 
                    "确定 / OK");
                return;
            }

            if (CoinTypePicker.SelectedItem == null)
            {
                await DisplayAlert("错误 / Error", 
                    "请选择币种 / Please select coin type", 
                    "确定 / OK");
                return;
            }

            try
            {
                if (_selectedCoinType == CoinType.Ethereum)
                {
                    await BuildEthereumTransaction();
                }
                else if (_selectedCoinType == CoinType.Bitcoin)
                {
                    await BuildBitcoinTransaction();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "构建交易时出错 / Error building transaction");
                await DisplayAlert("错误 / Error", 
                    $"构建交易时出错: {ex.Message} / Error building transaction: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 构建以太坊交易
        /// Build Ethereum transaction
        /// </summary>
        private async Task BuildEthereumTransaction()
        {
            // 验证输入
            // Validate input
            if (string.IsNullOrEmpty(EthToAddressEntry.Text))
            {
                await DisplayAlert("错误 / Error", 
                    "请输入接收方地址 / Please enter recipient address", 
                    "确定 / OK");
                return;
            }

            if (!decimal.TryParse(EthAmountEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
            {
                await DisplayAlert("错误 / Error", 
                    "请输入有效的金额 / Please enter a valid amount", 
                    "确定 / OK");
                return;
            }

            if (!decimal.TryParse(EthGasPriceEntry.Text, out decimal gasPrice) || gasPrice <= 0)
            {
                await DisplayAlert("错误 / Error", 
                    "请输入有效的Gas价格 / Please enter a valid gas price", 
                    "确定 / OK");
                return;
            }

            if (!uint.TryParse(EthGasLimitEntry.Text, out uint gasLimit) || gasLimit <= 0)
            {
                await DisplayAlert("错误 / Error", 
                    "请输入有效的Gas限制 / Please enter a valid gas limit", 
                    "确定 / OK");
                return;
            }

            // 创建交易对象
            // Create transaction object
            var transaction = new EthereumTransaction
            {
                From = EthFromAddressEntry.Text,
                To = EthToAddressEntry.Text,
                Value = Nethereum.Web3.Web3.Convert.ToWei(amount).ToString(),
                GasPrice = Nethereum.Web3.Web3.Convert.ToWei(gasPrice, Nethereum.Util.UnitConversion.EthUnit.Gwei).ToString(),
                GasLimit = gasLimit.ToString(),
                Data = string.IsNullOrEmpty(EthDataEntry.Text) ? "0x" : EthDataEntry.Text,
                ChainId = 1, // 主网 / Mainnet
                Nonce = "0" // 将由服务填充 / Will be filled by service
            };

            // 构建未签名交易
            // Build unsigned transaction
            _unsignedTransaction = await _walletService.BuildEthereumTransactionAsync(transaction);
            
            // 设置派生路径
            // Set derivation path
            _derivationPath = "m/44'/60'/0'/0/0"; // 以太坊标准路径 / Ethereum standard path
            
            // 显示交易摘要
            // Show transaction summary
            TransactionTypeLabel.Text = "以太坊交易 / Ethereum Transaction";
            FromAddressLabel.Text = $"从 / From: {transaction.From}";
            ToAddressLabel.Text = $"到 / To: {transaction.To}";
            AmountLabel.Text = $"金额 / Amount: {amount} ETH";
            FeeLabel.Text = $"Gas价格 / Gas Price: {gasPrice} Gwei";
            TotalLabel.Text = $"Gas限制 / Gas Limit: {gasLimit}";
            
            // 显示签名框架
            // Show signing frame
            Step2Frame_Content.IsVisible = true;
        }

        /// <summary>
        /// 构建比特币交易
        /// Build Bitcoin transaction
        /// </summary>
        private async Task BuildBitcoinTransaction()
        {
            // 验证输入
            // Validate input
            if (string.IsNullOrEmpty(BtcToAddressEntry.Text))
            {
                await DisplayAlert("错误 / Error", 
                    "请输入接收方地址 / Please enter recipient address", 
                    "确定 / OK");
                return;
            }

            if (!decimal.TryParse(BtcAmountEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
            {
                await DisplayAlert("错误 / Error", 
                    "请输入有效的金额 / Please enter a valid amount", 
                    "确定 / OK");
                return;
            }

            if (!decimal.TryParse(BtcFeeRateEntry.Text, out decimal feeRate) || feeRate <= 0)
            {
                await DisplayAlert("错误 / Error", 
                    "请输入有效的手续费率 / Please enter a valid fee rate", 
                    "确定 / OK");
                return;
            }

            // 创建交易对象
            // Create transaction object
            var transaction = new BitcoinTransaction
            {
                Version = 1,
                LockTime = 0
            };

            // 添加输出
            // Add output
            transaction.Outputs.Add(new BitcoinTransactionOutput
            {
                Address = BtcToAddressEntry.Text,
                Amount = (long)(amount * 100000000) // 转换为Satoshi / Convert to Satoshi
            });

            // 获取UTXO并添加输入
            // Get UTXOs and add inputs
            var utxos = await _walletService.GetBitcoinUtxosAsync(BtcFromAddressEntry.Text);
            
            long totalAmount = 0;
            _inputDerivationPaths = new Dictionary<int, string>();
            
            for (int i = 0; i < utxos.Count; i++)
            {
                var utxo = utxos[i];
                transaction.Inputs.Add(new BitcoinTransactionInput
                {
                    PreviousTxHash = utxo.TxId,
                    PreviousTxOutputIndex = utxo.OutputIndex,
                    Script = "", // 将由签名过程填充 / Will be filled by signing process
                    Amount = utxo.Amount
                });
                
                totalAmount += utxo.Amount;
                _inputDerivationPaths.Add(i, "m/44'/0'/0'/0/0"); // 比特币标准路径 / Bitcoin standard path
                
                // 如果已经有足够的UTXO，则停止添加
                // If we have enough UTXOs, stop adding
                if (totalAmount >= (long)(amount * 100000000) + (long)(feeRate * 250)) // 估计250字节的交易大小 / Estimate 250 bytes transaction size
                {
                    break;
                }
            }
            
            // 添加找零输出
            // Add change output
            long fee = (long)(feeRate * 250);
            long changeAmount = totalAmount - (long)(amount * 100000000) - fee;
            
            if (changeAmount > 0)
            {
                transaction.Outputs.Add(new BitcoinTransactionOutput
                {
                    Address = BtcFromAddressEntry.Text, // 找零地址 / Change address
                    Amount = changeAmount
                });
            }

            // 构建未签名交易
            // Build unsigned transaction
            _unsignedTransaction = await _walletService.BuildBitcoinTransactionAsync(transaction);
            
            // 显示交易摘要
            // Show transaction summary
            TransactionTypeLabel.Text = "比特币交易 / Bitcoin Transaction";
            FromAddressLabel.Text = $"从 / From: {BtcFromAddressEntry.Text}";
            ToAddressLabel.Text = $"到 / To: {BtcToAddressEntry.Text}";
            AmountLabel.Text = $"金额 / Amount: {amount} BTC";
            FeeLabel.Text = $"手续费 / Fee: {fee / 100000000.0} BTC";
            TotalLabel.Text = $"找零 / Change: {changeAmount / 100000000.0} BTC";
            
            // 显示签名框架
            // Show signing frame
            Step2Frame_Content.IsVisible = true;
        }

        /// <summary>
        /// 签名交易按钮点击事件
        /// Sign transaction button click event
        /// </summary>
        private async void SignTransactionButton_Clicked(object sender, EventArgs e)
        {
            if (_unsignedTransaction == null)
            {
                await DisplayAlert("错误 / Error", 
                    "请先构建交易 / Please build transaction first", 
                    "确定 / OK");
                return;
            }

            try
            {
                SigningIndicator.IsVisible = true;
                SigningIndicator.IsRunning = true;
                SignTransactionButton.IsEnabled = false;
                
                // 获取币种类型字符串
                // Get coin type string
                string coinType = _selectedCoinType == CoinType.Ethereum ? "eth" : "btc";
                
                // 将未签名交易转换为十六进制字符串
                // Convert unsigned transaction to hex string
                string unsignedTxHex = BitConverter.ToString(_unsignedTransaction).Replace("-", "");
                
                // 使用新的SignTransactionAsync方法
                // Use the new SignTransactionAsync method
                string signedTxHex = await _hardwareWalletManager.SignTransactionAsync(
                    coinType,
                    _derivationPath,
                    unsignedTxHex);
                
                // 将十六进制字符串转换回字节数组
                // Convert hex string back to byte array
                _signedTransaction = Enumerable.Range(0, signedTxHex.Length / 2)
                    .Select(x => Convert.ToByte(signedTxHex.Substring(x * 2, 2), 16))
                    .ToArray();
                
                await DisplayAlert("成功 / Success", 
                    "交易已成功签名 / Transaction signed successfully", 
                    "确定 / OK");
                
                NextToStep3Button.IsEnabled = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "签名交易时出错 / Error signing transaction");
                await DisplayAlert("错误 / Error", 
                    $"签名交易时出错: {ex.Message} / Error signing transaction: {ex.Message}", 
                    "确定 / OK");
            }
            finally
            {
                SigningIndicator.IsRunning = false;
                SigningIndicator.IsVisible = false;
                SignTransactionButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// 广播交易按钮点击事件
        /// Broadcast transaction button click event
        /// </summary>
        private async void BroadcastTransactionButton_Clicked(object sender, EventArgs e)
        {
            if (_signedTransaction == null)
            {
                await DisplayAlert("错误 / Error", 
                    "请先签名交易 / Please sign transaction first", 
                    "确定 / OK");
                return;
            }

            try
            {
                BroadcastTransactionButton.IsEnabled = false;
                
                if (_selectedCoinType == CoinType.Ethereum)
                {
                    _transactionId = await _walletService.BroadcastEthereumTransactionAsync(_signedTransaction);
                }
                else if (_selectedCoinType == CoinType.Bitcoin)
                {
                    _transactionId = await _walletService.BroadcastBitcoinTransactionAsync(_signedTransaction);
                }
                
                // 显示结果
                // Show result
                TransactionStatusLabel.Text = "交易已成功广播 / Transaction broadcasted successfully";
                TransactionStatusLabel.TextColor = Colors.Green;
                TransactionIdLabel.Text = _transactionId;
                
                Step3Frame_Content.IsVisible = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "广播交易时出错 / Error broadcasting transaction");
                await DisplayAlert("错误 / Error", 
                    $"广播交易时出错: {ex.Message} / Error broadcasting transaction: {ex.Message}", 
                    "确定 / OK");
                BroadcastTransactionButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// 新交易按钮点击事件
        /// New transaction button click event
        /// </summary>
        private void NewTransactionButton_Clicked(object sender, EventArgs e)
        {
            // 重置表单
            // Reset form
            _unsignedTransaction = null;
            _signedTransaction = null;
            _transactionId = null;
            
            EthToAddressEntry.Text = string.Empty;
            EthAmountEntry.Text = string.Empty;
            EthGasPriceEntry.Text = string.Empty;
            EthGasLimitEntry.Text = "21000";
            EthDataEntry.Text = string.Empty;
            
            BtcToAddressEntry.Text = string.Empty;
            BtcAmountEntry.Text = string.Empty;
            BtcFeeRateEntry.Text = string.Empty;
            
            // 隐藏结果框架
            // Hide result frame
            Step3Frame_Content.IsVisible = false;
            Step2Frame_Content.IsVisible = false;
            
            // 显示交易框架
            // Show transaction frame
            Step1Frame_Content.IsVisible = true;
        }

        /// <summary>
        /// 下一步到步骤2按钮点击事件
        /// Next to step 2 button click event
        /// </summary>
        private void NextToStep2Button_Clicked(object sender, EventArgs e)
        {
            // 切换到步骤2
            // Switch to step 2
            Step1Frame_Content.IsVisible = false;
            Step2Frame_Content.IsVisible = true;
            Step3Frame_Content.IsVisible = false;
            
            // 更新步骤指示器
            // Update step indicator
            UpdateStepIndicator(2);
        }

        /// <summary>
        /// 返回到步骤1按钮点击事件
        /// Back to step 1 button click event
        /// </summary>
        private void BackToStep1Button_Clicked(object sender, EventArgs e)
        {
            // 切换到步骤1
            // Switch to step 1
            Step1Frame_Content.IsVisible = true;
            Step2Frame_Content.IsVisible = false;
            Step3Frame_Content.IsVisible = false;
            
            // 更新步骤指示器
            // Update step indicator
            UpdateStepIndicator(1);
        }

        /// <summary>
        /// 下一步到步骤3按钮点击事件
        /// Next to step 3 button click event
        /// </summary>
        private void NextToStep3Button_Clicked(object sender, EventArgs e)
        {
            // 切换到步骤3
            // Switch to step 3
            Step1Frame_Content.IsVisible = false;
            Step2Frame_Content.IsVisible = false;
            Step3Frame_Content.IsVisible = true;
            
            // 更新步骤指示器
            // Update step indicator
            UpdateStepIndicator(3);
        }

        /// <summary>
        /// 返回到步骤2按钮点击事件
        /// Back to step 2 button click event
        /// </summary>
        private void BackToStep2Button_Clicked(object sender, EventArgs e)
        {
            // 切换到步骤2
            // Switch to step 2
            Step1Frame_Content.IsVisible = false;
            Step2Frame_Content.IsVisible = true;
            Step3Frame_Content.IsVisible = false;
            
            // 更新步骤指示器
            // Update step indicator
            UpdateStepIndicator(2);
        }

        /// <summary>
        /// 完成按钮点击事件
        /// Finish button click event
        /// </summary>
        private async void FinishButton_Clicked(object sender, EventArgs e)
        {
            // 返回到主页面
            // Return to main page
            await Navigation.PopAsync();
        }

        /// <summary>
        /// 更新步骤指示器
        /// Update step indicator
        /// </summary>
        private void UpdateStepIndicator(int currentStep)
        {
            // 更新步骤2的样式
            // Update step 2 style
            if (currentStep >= 2)
            {
                Step2Frame.BackgroundColor = (Color)Application.Current.Resources["Primary"];
                ((Label)Step2Frame.Content).TextColor = Colors.White;
                Line2.Stroke = (Color)Application.Current.Resources["Secondary"];
            }
            else
            {
                Step2Frame.BackgroundColor = (Color)Application.Current.Resources["Surface"];
                ((Label)Step2Frame.Content).TextColor = (Color)Application.Current.Resources["TextSecondary"];
                Line2.Stroke = (Color)Application.Current.Resources["Surface"];
            }
            
            // 更新步骤3的样式
            // Update step 3 style
            if (currentStep >= 3)
            {
                Step3Frame.BackgroundColor = (Color)Application.Current.Resources["Primary"];
                ((Label)Step3Frame.Content).TextColor = Colors.White;
            }
            else
            {
                Step3Frame.BackgroundColor = (Color)Application.Current.Resources["Surface"];
                ((Label)Step3Frame.Content).TextColor = (Color)Application.Current.Resources["TextSecondary"];
            }
        }
    }
} 