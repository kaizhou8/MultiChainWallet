using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace MultiChainWallet.UI.Pages
{
    /// <summary>
    /// 设置页面
    /// Settings page
    /// </summary>
    public partial class SettingsPage : ContentPage
    {
        public bool IsBiometricsEnabled { get; set; }

        public SettingsPage()
        {
            InitializeComponent();
            LoadSettings();
        }

        /// <summary>
        /// 加载设置
        /// Load settings
        /// </summary>
        private async void LoadSettings()
        {
            // Load saved settings from preferences
            IsBiometricsEnabled = Preferences.Default.Get("biometrics_enabled", false);
            BiometricsSwitch.IsToggled = IsBiometricsEnabled;

            // Load network settings
            string ethNetwork = Preferences.Default.Get("ethereum_network", "Mainnet");
            string btcNetwork = Preferences.Default.Get("bitcoin_network", "Mainnet");
            
            EthereumNetworkPicker.SelectedItem = ethNetwork == "Mainnet" ? "主网 / Mainnet" : "测试网 / Testnet";
            BitcoinNetworkPicker.SelectedItem = btcNetwork == "Mainnet" ? "主网 / Mainnet" : "测试网 / Testnet";

            // Load language setting
            string language = Preferences.Default.Get("language", "Chinese");
            LanguagePicker.SelectedItem = language == "Chinese" ? "中文 / Chinese" : "English / 英文";
        }

        /// <summary>
        /// 生物识别开关切换事件
        /// Biometrics switch toggled event
        /// </summary>
        private async void OnBiometricsSwitchToggled(object sender, ToggledEventArgs e)
        {
            if (e.Value)
            {
                bool isAvailable = await CheckBiometricsAvailability();
                if (!isAvailable)
                {
                    await DisplayAlert("提示 / Notice", 
                        "设备不支持生物识别或未设置 / Device does not support biometrics or not set up", 
                        "确定 / OK");
                    BiometricsSwitch.IsToggled = false;
                    return;
                }
            }

            IsBiometricsEnabled = e.Value;
            Preferences.Default.Set("biometrics_enabled", e.Value);
        }

        /// <summary>
        /// 检查生物识别是否可用
        /// Check if biometrics is available
        /// </summary>
        private async Task<bool> CheckBiometricsAvailability()
        {
            // Implementation depends on the specific biometrics plugin used
            return true; // Placeholder
        }

        /// <summary>
        /// 生物识别设置按钮点击事件
        /// Biometric settings button clicked event
        /// </summary>
        private async void OnBiometricSettingsClicked(object sender, EventArgs e)
        {
            // Navigate to biometric settings page
            await Shell.Current.GoToAsync("BiometricSettingsPage");
        }

        /// <summary>
        /// 修改密码按钮点击事件
        /// Change password button clicked event
        /// </summary>
        private async void OnChangePasswordClicked(object sender, EventArgs e)
        {
            // Navigate to change password page
            await Shell.Current.GoToAsync("ChangePasswordPage");
        }

        /// <summary>
        /// 备份钱包按钮点击事件
        /// Backup wallet button clicked event
        /// </summary>
        private async void OnBackupWalletClicked(object sender, EventArgs e)
        {
            // Navigate to backup wallet page
            await Shell.Current.GoToAsync("BackupWalletPage");
        }

        /// <summary>
        /// 钱包组管理按钮点击事件
        /// Wallet groups button clicked event
        /// </summary>
        private async void OnWalletGroupsClicked(object sender, EventArgs e)
        {
            // Navigate to wallet groups page
            await Shell.Current.GoToAsync("WalletGroupsPage");
        }

        /// <summary>
        /// 钱包标签管理按钮点击事件
        /// Wallet tags button clicked event
        /// </summary>
        private async void OnWalletTagsClicked(object sender, EventArgs e)
        {
            // Navigate to wallet tags page
            await Shell.Current.GoToAsync("WalletTagsPage");
        }

        /// <summary>
        /// 钱包导入导出按钮点击事件
        /// Wallet import/export button clicked event
        /// </summary>
        private async void OnWalletImportExportClicked(object sender, EventArgs e)
        {
            // Navigate to wallet import/export page
            await Shell.Current.GoToAsync("WalletImportExportPage");
        }

        /// <summary>
        /// 以太坊网络改变事件
        /// Ethereum network changed event
        /// </summary>
        private void OnEthereumNetworkChanged(object sender, EventArgs e)
        {
            if (EthereumNetworkPicker.SelectedItem == null)
                return;

            string network = EthereumNetworkPicker.SelectedItem.ToString().StartsWith("主网") ? "Mainnet" : "Testnet";
            Preferences.Default.Set("ethereum_network", network);
        }

        /// <summary>
        /// 比特币网络改变事件
        /// Bitcoin network changed event
        /// </summary>
        private void OnBitcoinNetworkChanged(object sender, EventArgs e)
        {
            if (BitcoinNetworkPicker.SelectedItem == null)
                return;

            string network = BitcoinNetworkPicker.SelectedItem.ToString().StartsWith("主网") ? "Mainnet" : "Testnet";
            Preferences.Default.Set("bitcoin_network", network);
        }

        /// <summary>
        /// 语言改变事件
        /// Language changed event
        /// </summary>
        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (LanguagePicker.SelectedItem == null)
                return;

            string language = LanguagePicker.SelectedItem.ToString().StartsWith("中文") ? "Chinese" : "English";
            Preferences.Default.Set("language", language);
        }
    }
}
