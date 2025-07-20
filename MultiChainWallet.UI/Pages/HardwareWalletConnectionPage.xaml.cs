using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using MultiChainWallet.Core.Interfaces;
using MultiChainWallet.Core.Models;
using MultiChainWallet.Infrastructure.Services;

namespace MultiChainWallet.UI.Pages
{
    /// <summary>
    /// 硬件钱包连接页面
    /// Hardware wallet connection page
    /// </summary>
    public partial class HardwareWalletConnectionPage : ContentPage
    {
        private readonly HardwareWalletManager _hardwareWalletManager;
        private readonly ILogger<HardwareWalletConnectionPage> _logger;
        private HardwareWalletType? _selectedWalletType;
        private bool _isConnected = false;
        private Dictionary<string, string> _connectionInstructions;
        private ObservableCollection<HardwareWalletListItem> _walletTypes = new ObservableCollection<HardwareWalletListItem>();
        private HardwareWalletDeviceInfo _currentDeviceInfo;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public HardwareWalletConnectionPage(
            HardwareWalletManager hardwareWalletManager,
            ILogger<HardwareWalletConnectionPage> logger)
        {
            InitializeComponent();
            _hardwareWalletManager = hardwareWalletManager;
            _logger = logger;
            
            // 初始化连接指南
            // Initialize connection instructions
            InitializeConnectionInstructions();
            
            // 设置初始状态
            // Set initial state
            SetupInitialState();
        }

        /// <summary>
        /// 初始化连接指南
        /// Initialize connection instructions
        /// </summary>
        private void InitializeConnectionInstructions()
        {
            _connectionInstructions = new Dictionary<string, string>
            {
                { 
                    "Ledger", 
                    "1. 确保您的Ledger设备已连接到电脑并已解锁\n2. 打开Ledger上的以太坊或比特币应用\n3. 点击"连接"按钮\n\n1. Make sure your Ledger device is connected to your computer and unlocked\n2. Open the Ethereum or Bitcoin app on your Ledger\n3. Click the \"Connect\" button" 
                },
                { 
                    "Trezor", 
                    "1. 确保您的Trezor设备已连接到电脑\n2. 点击"连接"按钮\n3. 按照Trezor设备上的提示进行操作\n\n1. Make sure your Trezor device is connected to your computer\n2. Click the \"Connect\" button\n3. Follow the prompts on your Trezor device" 
                },
                { 
                    "KeepKey", 
                    "1. 确保您的KeepKey设备已连接到电脑\n2. 点击"连接"按钮\n3. 按照KeepKey设备上的提示进行操作\n\n1. Make sure your KeepKey device is connected to your computer\n2. Click the \"Connect\" button\n3. Follow the prompts on your KeepKey device" 
                }
            };
        }

        /// <summary>
        /// 设置初始状态
        /// Set initial state
        /// </summary>
        private async void SetupInitialState()
        {
            // 设置硬件钱包类型列表
            // Set hardware wallet type list
            WalletTypeListView.ItemsSource = _walletTypes;
            
            // 设置初始UI状态
            // Set initial UI state
            ConnectionStatusLabel.Text = "未连接 / Not Connected";
            ConnectionStatusLabel.TextColor = Colors.Red;
            ConnectButton.IsEnabled = false;
            DisconnectButton.IsEnabled = false;
            DeviceInfoLayout.IsVisible = false;
            LoadingIndicator.IsVisible = false;
            
            // 获取支持的硬件钱包类型
            // Get supported hardware wallet types
            var supportedWalletTypes = _hardwareWalletManager.GetSupportedWalletTypes().ToList();
            
            foreach (var walletType in supportedWalletTypes)
            {
                _walletTypes.Add(new HardwareWalletListItem
                {
                    WalletType = walletType,
                    DisplayName = GetDisplayName(walletType),
                    ImageSource = GetImageSource(walletType)
                });
            }
            
            // 检查是否已有连接的硬件钱包
            // Check if there's already a connected hardware wallet
            if (_hardwareWalletManager.HasActiveWallet)
            {
                _isConnected = true;
                _selectedWalletType = _hardwareWalletManager.ActiveWalletType;
                
                // 更新UI状态
                // Update UI state
                UpdateUIForConnectedState();
                
                // 获取并显示设备信息
                // Get and display device info
                await FetchAndDisplayDeviceInfo();
            }
        }

        /// <summary>
        /// 获取硬件钱包类型的显示名称
        /// Get display name for hardware wallet type
        /// </summary>
        private string GetDisplayName(HardwareWalletType walletType)
        {
            return walletType.ToString();
        }

        /// <summary>
        /// 获取硬件钱包类型的图标
        /// Get icon for hardware wallet type
        /// </summary>
        private string GetImageSource(HardwareWalletType walletType)
        {
            return walletType switch
            {
                HardwareWalletType.Ledger => "ledger_icon.png",
                HardwareWalletType.Trezor => "trezor_icon.png",
                HardwareWalletType.KeepKey => "keepkey_icon.png",
                _ => "hardware_wallet_icon.png"
            };
        }

        /// <summary>
        /// 页面显示时调用
        /// Called when page appears
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // 检测连接的硬件钱包
            // Detect connected hardware wallets
            await DetectConnectedWallets();
        }

        /// <summary>
        /// 页面消失时调用
        /// Called when page disappears
        /// </summary>
        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            
            // 如果页面关闭但没有主动断开连接，保持连接状态
            // If page is closed without actively disconnecting, keep the connection state
        }

        /// <summary>
        /// 检测连接的硬件钱包
        /// Detect connected hardware wallets
        /// </summary>
        private async Task DetectConnectedWallets()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                StatusLabel.Text = "正在检测硬件钱包... / Detecting hardware wallets...";
                
                var connectedWallets = await _hardwareWalletManager.DetectConnectedWalletsAsync();
                
                if (connectedWallets.Count > 0)
                {
                    StatusLabel.Text = $"检测到 {connectedWallets.Count} 个硬件钱包 / Detected {connectedWallets.Count} hardware wallets";
                    
                    // 更新列表项状态
                    // Update list item status
                    foreach (var item in _walletTypes)
                    {
                        item.IsDetected = connectedWallets.Contains(item.WalletType);
                    }
                    
                    // 如果已选择了某个钱包类型，确认它是否被检测到
                    // If a wallet type is already selected, confirm if it's detected
                    if (_selectedWalletType.HasValue && connectedWallets.Contains(_selectedWalletType.Value))
                    {
                        ConnectButton.IsEnabled = true;
                    }
                }
                else
                {
                    StatusLabel.Text = "未检测到硬件钱包，请连接您的设备 / No hardware wallets detected, please connect your device";
                    
                    // 所有设备标记为未检测到
                    // Mark all devices as not detected
                    foreach (var item in _walletTypes)
                    {
                        item.IsDetected = false;
                    }
                    
                    ConnectButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检测硬件钱包时出错 / Error detecting hardware wallets");
                StatusLabel.Text = "检测硬件钱包时出错 / Error detecting hardware wallets";
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
            }
        }

        /// <summary>
        /// 硬件钱包类型选择改变时调用
        /// Called when hardware wallet type selection changes
        /// </summary>
        private void WalletTypeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.Count > 0)
            {
                var selectedItem = e.CurrentSelection[0] as HardwareWalletListItem;
                if (selectedItem != null)
                {
                    _selectedWalletType = selectedItem.WalletType;
                    
                    // 更新连接指南
                    // Update connection instructions
                    if (_connectionInstructions.TryGetValue(selectedItem.WalletType.ToString(), out string instructions))
                    {
                        InstructionsLabel.Text = instructions;
                    }
                    
                    // 启用连接按钮（如果设备已检测到）
                    // Enable connect button (if device is detected)
                    ConnectButton.IsEnabled = selectedItem.IsDetected;
                    
                    // 如果未检测到，显示提示
                    // If not detected, show hint
                    if (!selectedItem.IsDetected)
                    {
                        StatusLabel.Text = $"未检测到 {selectedItem.DisplayName}，请连接设备后点击"刷新" / {selectedItem.DisplayName} not detected, please connect the device and click \"Refresh\"";
                    }
                    else
                    {
                        StatusLabel.Text = $"已检测到 {selectedItem.DisplayName}，点击"连接"继续 / {selectedItem.DisplayName} detected, click \"Connect\" to continue";
                    }
                }
            }
            else
            {
                // 取消选择
                // Deselect
                _selectedWalletType = null;
                ConnectButton.IsEnabled = false;
                InstructionsLabel.Text = "请选择硬件钱包类型 / Please select a hardware wallet type";
                StatusLabel.Text = "未选择硬件钱包类型 / No hardware wallet type selected";
            }
        }

        /// <summary>
        /// 点击连接按钮时调用
        /// Called when connect button is clicked
        /// </summary>
        private async void ConnectButton_Clicked(object sender, EventArgs e)
        {
            if (!_selectedWalletType.HasValue)
            {
                await DisplayAlert("错误 / Error", "请先选择硬件钱包类型 / Please select a hardware wallet type first", "确定 / OK");
                return;
            }
            
            try
            {
                LoadingIndicator.IsVisible = true;
                StatusLabel.Text = $"正在连接到 {GetDisplayName(_selectedWalletType.Value)}... / Connecting to {GetDisplayName(_selectedWalletType.Value)}...";
                ConnectButton.IsEnabled = false;
                
                bool connected = await _hardwareWalletManager.ConnectWalletAsync(_selectedWalletType.Value);
                
                if (connected)
                {
                    _isConnected = true;
                    
                    // 更新UI状态
                    // Update UI state
                    UpdateUIForConnectedState();
                    
                    // 获取并显示设备信息
                    // Get and display device info
                    await FetchAndDisplayDeviceInfo();
                    
                    StatusLabel.Text = $"已成功连接到 {GetDisplayName(_selectedWalletType.Value)} / Successfully connected to {GetDisplayName(_selectedWalletType.Value)}";
                }
                else
                {
                    _isConnected = false;
                    StatusLabel.Text = $"连接到 {GetDisplayName(_selectedWalletType.Value)} 失败 / Failed to connect to {GetDisplayName(_selectedWalletType.Value)}";
                    ConnectButton.IsEnabled = true;
                    await DisplayAlert("连接失败 / Connection Failed", 
                        $"无法连接到 {GetDisplayName(_selectedWalletType.Value)}，请确保设备已连接并解锁，然后重试 / Could not connect to {GetDisplayName(_selectedWalletType.Value)}, please make sure the device is connected and unlocked, then try again", 
                        "确定 / OK");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "连接到硬件钱包时出错 / Error connecting to hardware wallet");
                StatusLabel.Text = "连接到硬件钱包时出错 / Error connecting to hardware wallet";
                ConnectButton.IsEnabled = true;
                await DisplayAlert("错误 / Error", "连接到硬件钱包时出错：" + ex.Message + " / Error connecting to hardware wallet: " + ex.Message, "确定 / OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
            }
        }

        /// <summary>
        /// 点击断开连接按钮时调用
        /// Called when disconnect button is clicked
        /// </summary>
        private async void DisconnectButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                StatusLabel.Text = "正在断开连接... / Disconnecting...";
                DisconnectButton.IsEnabled = false;
                
                await _hardwareWalletManager.DisconnectWalletAsync();
                
                _isConnected = false;
                
                // 更新UI状态
                // Update UI state
                UpdateUIForDisconnectedState();
                
                StatusLabel.Text = "已断开连接 / Disconnected";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "断开硬件钱包连接时出错 / Error disconnecting from hardware wallet");
                StatusLabel.Text = "断开连接时出错 / Error disconnecting";
                DisconnectButton.IsEnabled = true;
                await DisplayAlert("错误 / Error", "断开硬件钱包连接时出错：" + ex.Message + " / Error disconnecting from hardware wallet: " + ex.Message, "确定 / OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
            }
        }

        /// <summary>
        /// 点击刷新按钮时调用
        /// Called when refresh button is clicked
        /// </summary>
        private async void RefreshButton_Clicked(object sender, EventArgs e)
        {
            await DetectConnectedWallets();
        }

        /// <summary>
        /// 点击获取地址按钮时调用
        /// Called when get address button is clicked
        /// </summary>
        private async void GetAddressButton_Clicked(object sender, EventArgs e)
        {
            if (!_isConnected)
            {
                await DisplayAlert("错误 / Error", "请先连接硬件钱包 / Please connect to a hardware wallet first", "确定 / OK");
                return;
            }
            
            // 导航到地址页面
            // Navigate to address page
            await Navigation.PushAsync(new HardwareWalletAddressPage());
        }

        /// <summary>
        /// 点击签名交易按钮时调用
        /// Called when sign transaction button is clicked
        /// </summary>
        private async void SignTransactionButton_Clicked(object sender, EventArgs e)
        {
            if (!_isConnected)
            {
                await DisplayAlert("错误 / Error", "请先连接硬件钱包 / Please connect to a hardware wallet first", "确定 / OK");
                return;
            }
            
            // 导航到交易签名页面
            // Navigate to transaction signing page
            await Navigation.PushAsync(new HardwareWalletTransactionPage());
        }

        /// <summary>
        /// 更新UI为已连接状态
        /// Update UI for connected state
        /// </summary>
        private void UpdateUIForConnectedState()
        {
            ConnectionStatusLabel.Text = "已连接 / Connected";
            ConnectionStatusLabel.TextColor = Colors.Green;
            ConnectButton.IsEnabled = false;
            DisconnectButton.IsEnabled = true;
            WalletTypeListView.IsEnabled = false;
            GetAddressButton.IsEnabled = true;
            SignTransactionButton.IsEnabled = true;
            DeviceInfoLayout.IsVisible = true;
            RefreshButton.IsEnabled = false;
        }

        /// <summary>
        /// 更新UI为未连接状态
        /// Update UI for disconnected state
        /// </summary>
        private void UpdateUIForDisconnectedState()
        {
            ConnectionStatusLabel.Text = "未连接 / Not Connected";
            ConnectionStatusLabel.TextColor = Colors.Red;
            ConnectButton.IsEnabled = true;
            DisconnectButton.IsEnabled = false;
            WalletTypeListView.IsEnabled = true;
            GetAddressButton.IsEnabled = false;
            SignTransactionButton.IsEnabled = false;
            DeviceInfoLayout.IsVisible = false;
            RefreshButton.IsEnabled = true;
        }

        /// <summary>
        /// 获取并显示设备信息
        /// Fetch and display device info
        /// </summary>
        private async Task FetchAndDisplayDeviceInfo()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                StatusLabel.Text = "正在获取设备信息... / Getting device information...";
                
                _currentDeviceInfo = await _hardwareWalletManager.GetDeviceInfoAsync();
                
                // 显示设备信息
                // Display device info
                ModelLabel.Text = _currentDeviceInfo.Model;
                FirmwareLabel.Text = _currentDeviceInfo.FirmwareVersion;
                SerialNumberLabel.Text = _currentDeviceInfo.SerialNumber;
                StatusLabel.Text = "设备信息已更新 / Device information updated";
                
                // 更新安全状态
                // Update security status
                UpdateSecurityStatus(_currentDeviceInfo);
                
                // 显示设备信息区域
                // Show device info area
                DeviceInfoLayout.IsVisible = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备信息时出错 / Error getting device information");
                StatusLabel.Text = "获取设备信息时出错 / Error getting device information";
                await DisplayAlert("错误 / Error", "获取设备信息时出错：" + ex.Message + " / Error getting device information: " + ex.Message, "确定 / OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
            }
        }

        /// <summary>
        /// 更新安全状态
        /// Update security status
        /// </summary>
        private void UpdateSecurityStatus(HardwareWalletDeviceInfo deviceInfo)
        {
            // 更新安全状态标签
            // Update security status labels
            InitializedStatusLabel.Text = deviceInfo.IsInitialized ? "是 / Yes" : "否 / No";
            InitializedStatusLabel.TextColor = deviceInfo.IsInitialized ? Colors.Green : Colors.Red;
            
            UnlockedStatusLabel.Text = deviceInfo.IsUnlocked ? "是 / Yes" : "否 / No";
            UnlockedStatusLabel.TextColor = deviceInfo.IsUnlocked ? Colors.Green : Colors.Red;
            
            // 显示支持的功能
            // Display supported features
            FeaturesLabel.Text = string.Join(", ", deviceInfo.SupportedFeatures);
        }
    }
} 