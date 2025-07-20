using Microsoft.Maui.Controls;
using MultiChainWallet.Core.Models;
using MultiChainWallet.Core.Services;

namespace MultiChainWallet.UI.Pages
{
    /// <summary>
    /// 交易详情页面
    /// Transaction details page
    /// </summary>
    public partial class TransactionDetailsPage : ContentPage
    {
        private readonly IWalletService _walletService;
        private readonly TransactionInfo _transaction;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="walletService">钱包服务 / Wallet service</param>
        /// <param name="transaction">交易信息 / Transaction information</param>
        public TransactionDetailsPage(IWalletService walletService, TransactionInfo transaction)
        {
            InitializeComponent();
            _walletService = walletService;
            _transaction = transaction;
            LoadTransactionDetails();
        }

        /// <summary>
        /// 加载交易详情
        /// Load transaction details
        /// </summary>
        private void LoadTransactionDetails()
        {
            // 设置交易详情
            // Set transaction details
            TransactionHashLabel.Text = _transaction.Hash;
            FromAddressLabel.Text = _transaction.FromAddress;
            ToAddressLabel.Text = _transaction.ToAddress;
            AmountLabel.Text = _transaction.Amount.ToString();
            StatusLabel.Text = _transaction.Status.ToString();
            TimestampLabel.Text = _transaction.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
