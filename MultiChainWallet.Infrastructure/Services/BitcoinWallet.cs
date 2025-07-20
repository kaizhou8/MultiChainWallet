using System;
using System.Threading.Tasks;
using MultiChainWallet.Core.Interfaces;
using NBitcoin;
using NBitcoin.RPC;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 比特币钱包服务
    /// Bitcoin wallet service
    /// </summary>
    public class BitcoinWallet : IWallet
    {
        private readonly Network _network;
        private readonly RPCClient _rpcClient;
        private readonly BitcoinAddressValidator _addressValidator;
        private Key _privateKey;
        private bool _isLocked = true;
        private string _currentAddress;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public BitcoinWallet(Network network, string rpcUrl, string rpcUsername, string rpcPassword)
        {
            _network = network ?? Network.Main;
            _rpcClient = new RPCClient($"{rpcUsername}:{rpcPassword}", rpcUrl, _network);
            _addressValidator = new BitcoinAddressValidator(_network);
        }

        /// <summary>
        /// 创建新钱包
        /// Create new wallet
        /// </summary>
        public async Task<string> CreateWalletAsync()
        {
            _privateKey = new Key();
            _currentAddress = _privateKey.GetAddress(ScriptPubKeyType.Legacy, _network).ToString();
            return _currentAddress;
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
        public async Task<decimal> GetBalanceAsync(string address)
        {
            try
            {
                var bitcoinAddress = BitcoinAddress.Create(address, _network);
                var unspentCoins = await _rpcClient.ListUnspentAsync(0, 9999999, new[] { bitcoinAddress });
                
                decimal balance = 0;
                foreach (var coin in unspentCoins)
                {
                    balance += coin.Amount.ToDecimal(MoneyUnit.BTC);
                }

                return balance;
            }
            catch (Exception ex)
            {
                throw new Exception($"获取地址 {address} 的BTC余额失败 / Failed to get BTC balance for address {address}", ex);
            }
        }

        /// <summary>
        /// 发送交易
        /// Send transaction
        /// </summary>
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
                    throw new ArgumentException($"无效的接收地址: {validationResult.ErrorMessage} / Invalid receiving address: {validationResult.ErrorMessage}");
                }

                // 创建交易
                // Create transaction
                var transaction = Transaction.Create(_network);
                var money = Money.Coins(amount);
                var bitcoinAddress = BitcoinAddress.Create(toAddress, _network);
                var txOut = new TxOut(money, bitcoinAddress.ScriptPubKey);
                transaction.Outputs.Add(txOut);

                // 签名交易
                // Sign transaction
                var bitcoinSecret = new BitcoinSecret(_privateKey, _network);
                var coin = new Coin(new OutPoint(), new TxOut(money, bitcoinSecret.GetAddress(ScriptPubKeyType.Legacy)));
                transaction.Sign(bitcoinSecret, coin);

                // 广播交易
                // Broadcast transaction
                await _rpcClient.SendRawTransactionAsync(transaction);

                return transaction.GetHash().ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("BTC转账失败 / BTC transfer failed", ex);
            }
        }
    }
}
