using Microsoft.Maui.Controls;
using MultiChainWallet.Core.Services;

namespace MultiChainWallet.UI.Pages
{
    /// <summary>
    /// 修改密码页面
    /// Change password page
    /// </summary>
    public partial class ChangePasswordPage : ContentPage
    {
        private readonly IWalletService _walletService;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="walletService">钱包服务 / Wallet service</param>
        public ChangePasswordPage(IWalletService walletService)
        {
            InitializeComponent();
            _walletService = walletService;
        }

        /// <summary>
        /// 修改密码按钮点击事件
        /// Change password button click event
        /// </summary>
        private async void OnChangePasswordClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CurrentPasswordEntry.Text))
            {
                await DisplayAlert("错误 / Error", 
                    "请输入当前密码 / Please enter current password", 
                    "确定 / OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPasswordEntry.Text))
            {
                await DisplayAlert("错误 / Error", 
                    "请输入新密码 / Please enter new password", 
                    "确定 / OK");
                return;
            }

            if (NewPasswordEntry.Text != ConfirmPasswordEntry.Text)
            {
                await DisplayAlert("错误 / Error", 
                    "两次输入的新密码不一致 / New passwords do not match", 
                    "确定 / OK");
                return;
            }

            try
            {
                // 验证当前密码
                // Validate current password
                var isValid = await _walletService.ValidatePasswordAsync(CurrentPasswordEntry.Text);
                if (!isValid)
                {
                    await DisplayAlert("错误 / Error", 
                        "当前密码错误 / Current password is incorrect", 
                        "确定 / OK");
                    return;
                }

                // 修改密码
                // Change password
                await _walletService.ChangePasswordAsync(CurrentPasswordEntry.Text, NewPasswordEntry.Text);

                await DisplayAlert("成功 / Success", 
                    "密码修改成功 / Password changed successfully", 
                    "确定 / OK");

                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误 / Error", 
                    $"修改密码失败 / Failed to change password: {ex.Message}", 
                    "确定 / OK");
            }
        }
    }
}
