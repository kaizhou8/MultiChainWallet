using MultiChainWallet.Core.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MultiChainWallet.UI.Pages
{
    /// <summary>
    /// 钱包导入/导出页面
    /// Wallet import/export page
    /// </summary>
    public partial class WalletImportExportPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IWalletService _walletService;
        private string _statusMessage;
        private Color _statusColor;
        private bool _isStatusVisible;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="walletService">钱包服务 / Wallet service</param>
        public WalletImportExportPage(IWalletService walletService)
        {
            InitializeComponent();
            _walletService = walletService;
            BindingContext = this;
            IsStatusVisible = false;
        }

        /// <summary>
        /// 状态消息
        /// Status message
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 状态颜色
        /// Status color
        /// </summary>
        public Color StatusColor
        {
            get => _statusColor;
            set
            {
                _statusColor = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 状态是否可见
        /// Is status visible
        /// </summary>
        public bool IsStatusVisible
        {
            get => _isStatusVisible;
            set
            {
                _isStatusVisible = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 浏览导出路径按钮点击事件
        /// Browse export path button click event
        /// </summary>
        private async void OnBrowseExportPathClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".json" } },
                        { DevicePlatform.Android, new[] { "application/json" } },
                        { DevicePlatform.iOS, new[] { "public.json" } },
                        { DevicePlatform.macOS, new[] { "public.json" } },
                    }),
                    PickerTitle = "选择导出文件 / Select Export File"
                });

                if (result != null)
                {
                    ExportPathEntry.Text = result.FullPath;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误 / Error", 
                    $"选择文件失败 / Failed to select file: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 浏览导入路径按钮点击事件
        /// Browse import path button click event
        /// </summary>
        private async void OnBrowseImportPathClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".json" } },
                        { DevicePlatform.Android, new[] { "application/json" } },
                        { DevicePlatform.iOS, new[] { "public.json" } },
                        { DevicePlatform.macOS, new[] { "public.json" } },
                    }),
                    PickerTitle = "选择导入文件 / Select Import File"
                });

                if (result != null)
                {
                    ImportPathEntry.Text = result.FullPath;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误 / Error", 
                    $"选择文件失败 / Failed to select file: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 导出按钮点击事件
        /// Export button click event
        /// </summary>
        private async void OnExportClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ExportPathEntry.Text))
            {
                await DisplayAlert("提示 / Notice", 
                    "请选择导出路径 / Please select an export path", 
                    "确定 / OK");
                return;
            }

            try
            {
                var result = await _walletService.ExportWalletsToJsonAsync(ExportPathEntry.Text);
                if (result)
                {
                    StatusMessage = "导出成功 / Export successful";
                    StatusColor = Colors.Green;
                }
                else
                {
                    StatusMessage = "导出失败 / Export failed";
                    StatusColor = Colors.Red;
                }
                IsStatusVisible = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"导出失败 / Export failed: {ex.Message}";
                StatusColor = Colors.Red;
                IsStatusVisible = true;
            }
        }

        /// <summary>
        /// 导入按钮点击事件
        /// Import button click event
        /// </summary>
        private async void OnImportClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ImportPathEntry.Text))
            {
                await DisplayAlert("提示 / Notice", 
                    "请选择导入路径 / Please select an import path", 
                    "确定 / OK");
                return;
            }

            try
            {
                var count = await _walletService.ImportWalletsFromJsonAsync(
                    ImportPathEntry.Text, 
                    OverwriteCheckBox.IsChecked);
                
                if (count > 0)
                {
                    StatusMessage = $"成功导入 {count} 个钱包 / Successfully imported {count} wallets";
                    StatusColor = Colors.Green;
                }
                else
                {
                    StatusMessage = "没有导入任何钱包 / No wallets were imported";
                    StatusColor = Colors.Orange;
                }
                IsStatusVisible = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"导入失败 / Import failed: {ex.Message}";
                StatusColor = Colors.Red;
                IsStatusVisible = true;
            }
        }

        /// <summary>
        /// 属性变更事件
        /// Property changed event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 属性变更通知
        /// Property changed notification
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
