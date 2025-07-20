using Microsoft.Maui.Controls;
using MultiChainWallet.Core.Services;

namespace MultiChainWallet.UI.Pages
{
    /// <summary>
    /// 备份钱包页面
    /// Backup wallet page
    /// </summary>
    public partial class BackupWalletPage : ContentPage
    {
        private readonly IWalletService _walletService;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="walletService">钱包服务 / Wallet service</param>
        public BackupWalletPage(IWalletService walletService)
        {
            InitializeComponent();
            _walletService = walletService;
        }

        /// <summary>
        /// 选择路径按钮点击事件
        /// Select path button click event
        /// </summary>
        private async void OnSelectPathClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "选择备份文件保存位置 / Select backup file location",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".backup" } }
                    })
                });

                if (result != null)
                {
                    BackupPathEntry.Text = result.FullPath;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误 / Error", 
                    $"选择文件路径失败 / Failed to select file path: {ex.Message}", 
                    "确定 / OK");
            }
        }

        /// <summary>
        /// 备份按钮点击事件
        /// Backup button click event
        /// </summary>
        private async void OnBackupClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                await DisplayAlert("错误 / Error", 
                    "请输入密码 / Please enter password", 
                    "确定 / OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(BackupPathEntry.Text))
            {
                await DisplayAlert("错误 / Error", 
                    "请选择备份文件保存位置 / Please select backup file location", 
                    "确定 / OK");
                return;
            }

            try
            {
                // 验证密码
                // Validate password
                var isValid = await _walletService.ValidatePasswordAsync(PasswordEntry.Text);
                if (!isValid)
                {
                    await DisplayAlert("错误 / Error", 
                        "密码错误 / Password is incorrect", 
                        "确定 / OK");
                    return;
                }

                // 执行备份
                // Perform backup
                await _walletService.BackupWalletAsync(BackupPathEntry.Text, PasswordEntry.Text);

                await DisplayAlert("成功 / Success", 
                    "钱包备份成功 / Wallet backup successful", 
                    "确定 / OK");

                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误 / Error", 
                    $"备份钱包失败 / Failed to backup wallet: {ex.Message}", 
                    "确定 / OK");
            }
        }
    }
}
