using System.Collections.Generic;
using System.Threading.Tasks;
using MultiChainWallet.Core.Models;

namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// ERC20代币服务接口
    /// ERC20 token service interface
    /// </summary>
    public interface IERC20Service
    {
        // Token Management
        Task<bool> AddToken(string contractAddress);
        Task<bool> RemoveToken(string contractAddress);
        Task<List<ERC20TokenInfo>> GetUserTokens(string walletAddress);
        
        // Balance and Price
        Task<decimal> GetTokenBalance(string contractAddress, string walletAddress);
        Task<decimal> GetTokenPrice(string contractAddress);
        Task UpdateAllTokenBalances(string walletAddress);
        
        // Transactions
        Task<string> Transfer(string contractAddress, string toAddress, decimal amount);
        Task<List<ERC20TransactionHistory>> GetTransactionHistory(string contractAddress, string walletAddress);
        Task<bool> ApproveSpender(string contractAddress, string spenderAddress, decimal amount);
        Task<decimal> GetAllowance(string contractAddress, string ownerAddress, string spenderAddress);
        
        // Token Information
        Task<ERC20TokenInfo> GetTokenInfo(string contractAddress);
        Task<bool> ValidateTokenContract(string contractAddress);
        
        // Persistence
        Task SaveTokenList(string walletAddress, List<string> tokenAddresses);
        Task<List<string>> LoadTokenList(string walletAddress);

        // Extended Features
        Task<bool> IsValidAddress(string address);
        Task<decimal> EstimateGasFee(string contractAddress, string toAddress, decimal amount);
        Task<bool> SendToken(string contractAddress, string toAddress, decimal amount, string password);
        Task<List<TokenTransaction>> GetTokenTransactions(string contractAddress, string walletAddress);
    }

    /// <summary>
    /// 代币交易历史记录
    /// Token transaction history record
    /// </summary>
    public class TokenTransaction
    {
        public string Type { get; set; }
        public string Address { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Symbol { get; set; }
    }
}
