using System;
using System.Numerics;
using System.Threading.Tasks;
using MultiChainWallet.Core.Interfaces;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexTypes;
using Nethereum.Util;
using Nethereum.Signer;
using Org.BouncyCastle.Security;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 以太坊钱包实现
    /// Ethereum wallet implementation
    /// </summary>
    public class EthereumWallet : IWallet
    {
        private readonly string _infuraUrl;
        private readonly Web3 _web3;
        private readonly EthereumAddressValidator _addressValidator;
        private Account _account;
        private bool _isLocked = true;
        private string _currentAddress;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="infuraUrl">Infura节点URL / Infura node URL</param>
        public EthereumWallet(string infuraUrl)
        {
            _infuraUrl = infuraUrl;
            _web3 = new Web3(infuraUrl);
            _addressValidator = new EthereumAddressValidator();
        }

        /// <summary>
        /// 创建新钱包
        /// Create a new wallet
        /// </summary>
        /// <returns>钱包地址 / Wallet address</returns>
        public async Task<string> CreateWalletAsync()
        {
            try
            {
                var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
                _account = new Account(ecKey.GetPrivateKeyAsBytes());
                _currentAddress = _account.Address;
                return _currentAddress;
            }
            catch (Exception ex)
            {
                throw new Exception("创建ETH钱包失败 / Failed to create ETH wallet", ex);
            }
        }

        /// <summary>
        /// 获取当前钱包地址
        /// Get current wallet address
        /// </summary>
        public async Task<string> GetCurrentAddress()
        {
            if (string.IsNullOrEmpty(_currentAddress))
            {
                throw new InvalidOperationException("Wallet has not been created or loaded");
            }
            return _currentAddress;
        }

        /// <summary>
        /// 解锁钱包
        /// Unlock wallet
        /// </summary>
        public async Task<bool> UnlockWallet(string password)
        {
            // 这里应该实现实际的解锁逻辑
            // Implement actual unlock logic here
            _isLocked = false;
            return true;
        }

        /// <summary>
        /// 锁定钱包
        /// Lock wallet
        /// </summary>
        public async Task<bool> LockWallet()
        {
            _isLocked = true;
            return true;
        }

        /// <summary>
        /// 验证密码
        /// Validate password
        /// </summary>
        public async Task<bool> ValidatePassword(string password)
        {
            // 这里应该实现实际的密码验证逻辑
            // Implement actual password validation logic here
            return true;
        }

        /// <summary>
        /// 获取钱包余额
        /// Get wallet balance
        /// </summary>
        /// <param name="address">钱包地址 / Wallet address</param>
        /// <returns>余额（ETH） / Balance (ETH)</returns>
        public async Task<decimal> GetBalanceAsync(string address)
        {
            try
            {
                // 验证地址
                // Validate address
                var validationResult = await _addressValidator.ValidateAddress(address);
                if (!validationResult.IsValid)
                {
                    throw new ArgumentException($"无效的ETH地址 / Invalid ETH address: {validationResult.ErrorMessage}");
                }

                var balance = await _web3.Eth.GetBalance.SendRequestAsync(address);
                return Web3.Convert.FromWei(balance.Value);
            }
            catch (Exception ex)
            {
                throw new Exception($"获取地址 {address} 的ETH余额失败 / Failed to get ETH balance for address {address}", ex);
            }
        }

        /// <summary>
        /// 发送交易
        /// Send transaction
        /// </summary>
        /// <param name="fromAddress">发送地址 / From address</param>
        /// <param name="toAddress">接收地址 / To address</param>
        /// <param name="amount">金额（ETH） / Amount (ETH)</param>
        /// <returns>交易哈希 / Transaction hash</returns>
        public async Task<string> SendTransactionAsync(string fromAddress, string toAddress, decimal amount)
        {
            if (_isLocked)
            {
                throw new InvalidOperationException("Wallet is locked");
            }

            try
            {
                // 验证接收地址
                // Validate receiving address
                var validationResult = await _addressValidator.ValidateAddress(toAddress);
                if (!validationResult.IsValid)
                {
                    throw new ArgumentException($"无效的接收地址 / Invalid receiving address: {validationResult.ErrorMessage}");
                }

                // 检查是否发送到合约地址
                // Check if sending to contract address
                if (validationResult.AddressType == "Contract")
                {
                    throw new ArgumentException("不支持直接向合约地址发送ETH / Direct ETH transfer to contract address is not supported");
                }

                var web3 = new Web3(_account, _infuraUrl);
                var weiAmount = Web3.Convert.ToWei(amount);
                var gas = new HexBigInteger(21000); // 标准ETH转账的gas限制 / Standard gas limit for ETH transfer
                
                var transaction = await web3.Eth.GetEtherTransferService()
                    .TransferEtherAsync(toAddress, amount);

                return transaction;
            }
            catch (Exception ex)
            {
                throw new Exception("ETH转账失败 / ETH transfer failed", ex);
            }
        }

        /// <summary>
        /// 使用私钥发送交易
        /// Send transaction with private key
        /// </summary>
        /// <param name="privateKey">私钥 / Private key</param>
        /// <param name="toAddress">接收地址 / To address</param>
        /// <param name="amount">金额（ETH） / Amount (ETH)</param>
        /// <returns>交易哈希 / Transaction hash</returns>
        public async Task<string> SendTransactionWithPrivateKeyAsync(string privateKey, string toAddress, decimal amount)
        {
            try
            {
                // 验证接收地址
                // Validate receiving address
                var validationResult = await _addressValidator.ValidateAddress(toAddress);
                if (!validationResult.IsValid)
                {
                    throw new ArgumentException($"无效的接收地址 / Invalid receiving address: {validationResult.ErrorMessage}");
                }

                // 检查是否发送到合约地址
                // Check if sending to contract address
                if (validationResult.AddressType == "Contract")
                {
                    throw new ArgumentException("不支持直接向合约地址发送ETH / Direct ETH transfer to contract address is not supported");
                }

                var account = new Account(privateKey);
                var web3 = new Web3(account, _infuraUrl);

                var value = Web3.Convert.ToWei(amount);
                var gas = new HexBigInteger(21000); // 标准ETH转账的gas限制 / Standard gas limit for ETH transfer
                
                var transaction = await web3.Eth.GetEtherTransferService()
                    .TransferEtherAsync(toAddress, amount);

                return transaction;
            }
            catch (Exception ex)
            {
                throw new Exception("ETH转账失败 / ETH transfer failed", ex);
            }
        }
    }
}
