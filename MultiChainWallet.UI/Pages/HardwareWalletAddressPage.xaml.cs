using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Models;
using MultiChainWallet.Core.Services;
using MultiChainWallet.Infrastructure.Services;

namespace MultiChainWallet.UI.Pages
{
    /// <summary>
    /// 硬件钱包地址页面
    /// Hardware wallet address page
    /// </summary>
    public partial class HardwareWalletAddressPage : ContentPage
    {
        private readonly HardwareWalletManager _hardwareWalletManager;
        private readonly IWalletService _walletService;
        private readonly ILogger<HardwareWalletAddressPage> _logger;
        private CoinType _selectedCoinType;
        private string _currentAddress;
        private string _currentDerivationPath;
        private bool _isConnected = false;
        private ObservableCollection<HardwareWalletAddressItem> _addresses = new ObservableCollection<HardwareWalletAddressItem>();

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public HardwareWalletAddressPage(
            HardwareWalletManager hardwareWalletManager,
            IWalletService walletService,
            ILogger<HardwareWalletAddressPage> logger)
        {
            InitializeComponent();
            _hardwareWalletManager = hardwareWalletManager;
            _walletService = walletService;
            _logger = logger;
            
            AddressCollectionView.ItemsSource = _addresses;
        }

        /// <summary>
        /// 页面出现时
        /// When page appears
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            CheckConnectionStatus();
            LoadImportedAddresses();
        }

        /// <summary>
        /// 检查连接状态
        /// Check connection status
        /// </summary>
        private void CheckConnectionStatus()
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
                    
                    // 显示地址框架
                    // Show address frame
                    AddressFrame.IsVisible = true;
                }
                else
                {
                    _isConnected = false;
                    ConnectionStatusLabel.Text = "未连接 / Not Connected";
                    ConnectionStatusLabel.TextColor = Colors.Red;
                    ConnectWalletButton.Text = "连接硬件钱包 / Connect Hardware Wallet";
                    
                    // 隐藏地址框架
                    // Hide address frame
                    AddressFrame.IsVisible = false;
                    AddressResultFrame.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查连接状态时出错 / Error checking connection status");
            }
        }

        /// <summary>
        /// 加载已导入地址
        /// Load imported addresses
        /// </summary>
        private async void LoadImportedAddresses()
        {
            try
            {
                var importedAddresses = await _walletService.GetHardwareWalletAddressesAsync();
                
                _addresses.Clear();
                foreach (var address in importedAddresses)
                {
                    _addresses.Add(new HardwareWalletAddressItem
                    {
                        Address = address.Address,
                        Path = address.DerivationPath,
                        CoinType = address.CoinType
                    });
                }
                
                AddressListFrame.IsVisible = _addresses.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载已导入地址时出错 / Error loading imported addresses");
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
                }
                else if (selectedItem.Contains("比特币"))
                {
                    _selectedCoinType = CoinType.Bitcoin;
                }
            }
        }

        /// <summary>
        /// 获取地址按钮点击事件
        /// Get address button click event
        /// </summary>
        private async void GetAddressButton_Clicked(object sender, EventArgs e)
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

            if (!uint.TryParse(AccountIndexEntry.Text, out uint accountIndex))
            {
                await DisplayAlert("错误 / Error", 
                    "请输入有效的账户索引 / Please enter a valid account index", 
                    "确定 / OK");
                return;
            }

            if (!uint.TryParse(AddressIndexEntry.Text, out uint addressIndex))
            {
                await DisplayAlert("错误 / Error", 
                    "请输入有效的地址索引 / Please enter a valid address index", 
                    "确定 / OK");
                return;
            }

            try
            {
                AddressIndicator.IsVisible = true;
                AddressIndicator.IsRunning = true;
                GetAddressButton.IsEnabled = false;
                
                // 获取派生路径
                // Get derivation path
                string derivationPath = GetDerivationPath(_selectedCoinType, accountIndex, addressIndex);
                
                // 获取币种类型字符串
                // Get coin type string
                string coinType = _selectedCoinType == CoinType.Ethereum ? "eth" : "btc";
                
                // 获取地址，使用新的接口方法
                // Get address, using the new interface method
                string address = await _hardwareWalletManager.GetAddressAsync(
                    coinType, 
                    derivationPath, 
                    false); // 不在设备上显示 / Don't show on device
                
                // 显示地址信息
                // Show address information
                CoinTypeLabel.Text = _selectedCoinType.ToString();
                DerivationPathLabel.Text = derivationPath;
                AddressLabel.Text = address;
                
                // 保存当前地址和派生路径
                // Save current address and derivation path
                _currentAddress = address;
                _currentDerivationPath = derivationPath;
                
                // 显示地址结果框架
                // Show address result frame
                AddressResultFrame.IsVisible = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取地址时出错 / Error getting address");
                await DisplayAlert("错误 / Error", 
                    $"获取地址时出错: {ex.Message} / Error getting address: {ex.Message}", 
                    "确定 / OK");
            }
            finally
            {
                AddressIndicator.IsRunning = false;
                AddressIndicator.IsVisible = false;
                GetAddressButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// 获取派生路径
        /// Get derivation path
        /// </summary>
        private string GetDerivationPath(CoinType coinType, uint accountIndex, uint addressIndex)
        {
            return coinType switch
            {
                CoinType.Ethereum => $"m/44'/60'/{accountIndex}'/0/{addressIndex}",
                CoinType.Bitcoin => $"m/44'/0'/{accountIndex}'/0/{addressIndex}",
                _ => $"m/44'/0'/{accountIndex}'/0/{addressIndex}"
            };
        }

        /// <summary>
        /// 验证地址按钮点击事件
        /// Verify address button click event
        /// </summary>
        private async void VerifyAddressButton_Clicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentAddress) || string.IsNullOrEmpty(_currentDerivationPath))
            {
                await DisplayAlert("错误 / Error", 
                    "请先获取地址 / Please get address first", 
                    "确定 / OK");
                return;
            }

            try
            {
                // 获取币种类型字符串
                // Get coin type string
                string coinType = _selectedCoinType == CoinType.Ethereum ? "eth" : "btc";
                
                // 在设备上显示地址进行验证
                // Display address on device for verification
                string verifiedAddress = await _hardwareWalletManager.GetAddressAsync(
                    coinType, 
                    _currentDerivationPath, 
                    true); // 在设备上显示 / Show on device
                
                bool verified = _currentAddress.Equals(verifiedAddress, StringComparison.OrdinalIgnoreCase);
                
                if (verified)
                {
                    await DisplayAlert("成功 / Success", 
                        "地址已在设备上验证 / Address verified on device", 
                        "确定 / OK");
                }
                else
                {
                    await DisplayAlert("警告 / Warning", 
                        "地址验证失败 / Address verification failed", 
                        "确定 / OK");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证地址时出错 / Error verifying address");
                await DisplayAlert("错误 / Error", 
                    $"验证地址时出错: {ex.Message} / Error verifying address: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 复制地址按钮点击事件
        /// Copy address button click event
        /// </summary>
        private async void CopyAddressButton_Clicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentAddress))
            {
                await DisplayAlert("错误 / Error", 
                    "请先获取地址 / Please get address first", 
                    "确定 / OK");
                return;
            }

            try
            {
                await Clipboard.SetTextAsync(_currentAddress);
                await DisplayAlert("成功 / Success", 
                    "地址已复制到剪贴板 / Address copied to clipboard", 
                    "确定 / OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "复制地址时出错 / Error copying address");
                await DisplayAlert("错误 / Error", 
                    $"复制地址时出错: {ex.Message} / Error copying address: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 导入地址按钮点击事件
        /// Import address button click event
        /// </summary>
        private async void ImportAddressButton_Clicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentAddress) || string.IsNullOrEmpty(_currentDerivationPath))
            {
                await DisplayAlert("错误 / Error", 
                    "请先获取地址 / Please get address first", 
                    "确定 / OK");
                return;
            }

            try
            {
                // 导入地址
                // Import address
                await _walletService.ImportHardwareWalletAddressAsync(
                    _currentAddress, 
                    _currentDerivationPath, 
                    _selectedCoinType, 
                    _hardwareWalletManager.GetConnectedWalletType().Value);
                
                // 刷新地址列表
                // Refresh address list
                LoadImportedAddresses();
                
                await DisplayAlert("成功 / Success", 
                    "地址已导入 / Address imported", 
                    "确定 / OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入地址时出错 / Error importing address");
                await DisplayAlert("错误 / Error", 
                    $"导入地址时出错: {ex.Message} / Error importing address: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 验证已导入地址按钮点击事件
        /// Verify imported address button click event
        /// </summary>
        private async void VerifyImportedAddress_Clicked(object sender, EventArgs e)
        {
            if (!_isConnected)
            {
                await DisplayAlert("错误 / Error", 
                    "请先连接硬件钱包 / Please connect to hardware wallet first", 
                    "确定 / OK");
                return;
            }

            if (sender is Button button && button.CommandParameter is HardwareWalletAddressItem item)
            {
                try
                {
                    // 获取币种类型字符串
                    // Get coin type string
                    string coinType = item.CoinType == CoinType.Ethereum ? "eth" : "btc";
                    
                    // 在设备上显示地址进行验证
                    // Display address on device for verification
                    string verifiedAddress = await _hardwareWalletManager.GetAddressAsync(
                        coinType, 
                        item.Path, 
                        true); // 在设备上显示 / Show on device
                    
                    bool verified = item.Address.Equals(verifiedAddress, StringComparison.OrdinalIgnoreCase);
                    
                    if (verified)
                    {
                        await DisplayAlert("成功 / Success", 
                            "地址已在设备上验证 / Address verified on device", 
                            "确定 / OK");
                    }
                    else
                    {
                        await DisplayAlert("警告 / Warning", 
                            "地址验证失败 / Address verification failed", 
                            "确定 / OK");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "验证已导入地址时出错 / Error verifying imported address");
                    await DisplayAlert("错误 / Error", 
                        $"验证地址时出错: {ex.Message} / Error verifying address: {ex.Message}", 
                        "确定 / OK");
                }
            }
        }
    }

    /// <summary>
    /// 硬件钱包地址项
    /// Hardware wallet address item
    /// </summary>
    public class HardwareWalletAddressItem
    {
        /// <summary>
        /// 地址
        /// Address
        /// </summary>
        public string Address { get; set; }
        
        /// <summary>
        /// 派生路径
        /// Derivation path
        /// </summary>
        public string Path { get; set; }
        
        /// <summary>
        /// 币种类型
        /// Coin type
        /// </summary>
        public CoinType CoinType { get; set; }
    }
} 