using System;
using System.Threading.Tasks;
using MultiChainWallet.Core.Interfaces;
using NBitcoin;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 比特币地址验证器
    /// Bitcoin address validator
    /// </summary>
    public class BitcoinAddressValidator : IAddressValidator
    {
        private readonly Network _network;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="network">比特币网络类型 / Bitcoin network type</param>
        public BitcoinAddressValidator(Network network = null)
        {
            _network = network ?? Network.Main;
        }

        /// <summary>
        /// 验证比特币地址
        /// Validate Bitcoin address
        /// </summary>
        /// <param name="address">要验证的地址 / Address to validate</param>
        /// <returns>验证结果 / Validation result</returns>
        public async Task<AddressValidationResult> ValidateAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return new AddressValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "地址不能为空 / Address cannot be empty",
                    AddressType = "Unknown",
                    Network = _network.Name
                };
            }

            try
            {
                var bitcoinAddress = BitcoinAddress.Create(address, _network);
                var scriptPubKey = bitcoinAddress.ScriptPubKey;

                string addressType = "Unknown";
                if (scriptPubKey.IsScriptType(ScriptType.P2PKH))
                {
                    addressType = "P2PKH (Legacy)";
                }
                else if (scriptPubKey.IsScriptType(ScriptType.P2SH))
                {
                    addressType = "P2SH";
                }
                else if (scriptPubKey.IsScriptType(ScriptType.Witness))
                {
                    addressType = "Native SegWit";
                }

                return new AddressValidationResult
                {
                    IsValid = true,
                    ErrorMessage = string.Empty,
                    AddressType = addressType,
                    Network = _network.Name
                };
            }
            catch (Exception ex)
            {
                return new AddressValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"无效的比特币地址: {ex.Message} / Invalid Bitcoin address: {ex.Message}",
                    AddressType = "Unknown",
                    Network = _network.Name
                };
            }
        }

        /// <summary>
        /// 验证地址是否属于指定网络
        /// Validate if address belongs to specified network
        /// </summary>
        /// <param name="address">要验证的地址 / Address to validate</param>
        /// <param name="expectedNetwork">期望的网络类型 / Expected network type</param>
        /// <returns>验证结果 / Validation result</returns>
        public bool ValidateAddressNetwork(string address, Network expectedNetwork)
        {
            try
            {
                var bitcoinAddress = BitcoinAddress.Create(address, expectedNetwork);
                return bitcoinAddress.Network == expectedNetwork;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取地址的脚本类型
        /// Get script type of address
        /// </summary>
        /// <param name="address">比特币地址 / Bitcoin address</param>
        /// <returns>脚本类型 / Script type</returns>
        public ScriptType? GetAddressScriptType(string address)
        {
            try
            {
                var bitcoinAddress = BitcoinAddress.Create(address, _network);
                var scriptPubKey = bitcoinAddress.ScriptPubKey;
                
                if (scriptPubKey.IsScriptType(ScriptType.P2PKH))
                    return ScriptType.P2PKH;
                if (scriptPubKey.IsScriptType(ScriptType.P2SH))
                    return ScriptType.P2SH;
                if (scriptPubKey.IsScriptType(ScriptType.P2WPKH))
                    return ScriptType.P2WPKH;
                if (scriptPubKey.IsScriptType(ScriptType.P2WSH))
                    return ScriptType.P2WSH;
                
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
