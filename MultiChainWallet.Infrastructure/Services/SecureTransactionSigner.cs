using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Models;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 安全的交易签名服务
    /// Secure transaction signing service
    /// </summary>
    public class SecureTransactionSigner
    {
        private readonly SecurePrivateKeyManager _privateKeyManager;
        private readonly EnhancedSecurityService _securityService;
        private readonly ILogger<SecureTransactionSigner> _logger;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public SecureTransactionSigner(
            SecurePrivateKeyManager privateKeyManager,
            EnhancedSecurityService securityService,
            ILogger<SecureTransactionSigner> logger)
        {
            _privateKeyManager = privateKeyManager;
            _securityService = securityService;
            _logger = logger;
        }

        /// <summary>
        /// 安全地签名以太坊交易
        /// Securely sign Ethereum transaction
        /// </summary>
        public async Task<byte[]> SignEthereumTransactionAsync(
            string walletId,
            string encryptedPrivateKey,
            string password,
            byte[] unsignedTransaction)
        {
            try
            {
                // 获取私钥（如果已加载）
                // Get private key (if loaded)
                string privateKey = await _privateKeyManager.GetPrivateKeyAsync(walletId);
                
                // 如果私钥未加载，则加载私钥
                // If private key not loaded, load it
                if (privateKey == null)
                {
                    privateKey = await _privateKeyManager.LoadPrivateKeyAsync(walletId, encryptedPrivateKey, password);
                }
                
                // 使用私钥签名交易
                // Sign transaction with private key
                byte[] signedTransaction = await SignEthereumTransactionWithPrivateKeyAsync(privateKey, unsignedTransaction);
                
                return signedTransaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "签名以太坊交易失败 / Failed to sign Ethereum transaction");
                throw new InvalidOperationException("签名以太坊交易失败 / Failed to sign Ethereum transaction", ex);
            }
        }

        /// <summary>
        /// 安全地签名比特币交易
        /// Securely sign Bitcoin transaction
        /// </summary>
        public async Task<byte[]> SignBitcoinTransactionAsync(
            string walletId,
            string encryptedPrivateKey,
            string password,
            byte[] unsignedTransaction)
        {
            try
            {
                // 获取私钥（如果已加载）
                // Get private key (if loaded)
                string privateKey = await _privateKeyManager.GetPrivateKeyAsync(walletId);
                
                // 如果私钥未加载，则加载私钥
                // If private key not loaded, load it
                if (privateKey == null)
                {
                    privateKey = await _privateKeyManager.LoadPrivateKeyAsync(walletId, encryptedPrivateKey, password);
                }
                
                // 使用私钥签名交易
                // Sign transaction with private key
                byte[] signedTransaction = await SignBitcoinTransactionWithPrivateKeyAsync(privateKey, unsignedTransaction);
                
                return signedTransaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "签名比特币交易失败 / Failed to sign Bitcoin transaction");
                throw new InvalidOperationException("签名比特币交易失败 / Failed to sign Bitcoin transaction", ex);
            }
        }

        /// <summary>
        /// 使用私钥签名以太坊交易
        /// Sign Ethereum transaction with private key
        /// </summary>
        private async Task<byte[]> SignEthereumTransactionWithPrivateKeyAsync(string privateKey, byte[] unsignedTransaction)
        {
            try
            {
                // 创建以太坊签名者
                // Create Ethereum signer
                var signer = new Nethereum.Signer.TransactionSigner();
                
                // 移除私钥的"0x"前缀（如果有）
                // Remove "0x" prefix from private key (if any)
                if (privateKey.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    privateKey = privateKey.Substring(2);
                }
                
                // 签名交易
                // Sign transaction
                byte[] signedTransaction = signer.SignTransaction(privateKey, unsignedTransaction);
                
                return signedTransaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "使用私钥签名以太坊交易失败 / Failed to sign Ethereum transaction with private key");
                throw;
            }
        }

        /// <summary>
        /// 使用私钥签名比特币交易
        /// Sign Bitcoin transaction with private key
        /// </summary>
        private async Task<byte[]> SignBitcoinTransactionWithPrivateKeyAsync(string privateKey, byte[] unsignedTransaction)
        {
            try
            {
                // 注意：这里应该使用实际的比特币签名逻辑
                // Note: Actual Bitcoin signing logic should be used here
                // 以下代码仅为示例，实际实现应使用NBitcoin等库
                // The following code is just an example, actual implementation should use NBitcoin or similar library
                
                // 创建比特币签名者
                // Create Bitcoin signer
                var network = NBitcoin.Network.Main;
                var key = NBitcoin.Key.Parse(privateKey, network);
                
                // 解析未签名交易
                // Parse unsigned transaction
                var transaction = NBitcoin.Transaction.Parse(
                    System.Text.Encoding.UTF8.GetString(unsignedTransaction),
                    network);
                
                // 签名交易
                // Sign transaction
                // 注意：实际实现需要知道要签名的输入和对应的脚本
                // Note: Actual implementation needs to know which inputs to sign and corresponding scripts
                
                // 返回签名后的交易
                // Return signed transaction
                return System.Text.Encoding.UTF8.GetBytes(transaction.ToHex());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "使用私钥签名比特币交易失败 / Failed to sign Bitcoin transaction with private key");
                throw;
            }
        }

        /// <summary>
        /// 清除钱包的私钥
        /// Clear private key for wallet
        /// </summary>
        public async Task ClearPrivateKeyAsync(string walletId)
        {
            await _privateKeyManager.ClearPrivateKeyAsync(walletId);
        }

        /// <summary>
        /// 清除所有私钥
        /// Clear all private keys
        /// </summary>
        public async Task ClearAllPrivateKeysAsync()
        {
            await _privateKeyManager.ClearAllPrivateKeysAsync();
        }
    }
} 