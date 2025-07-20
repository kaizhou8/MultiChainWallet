using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MultiChainWallet.Core.Interfaces;
using Nethereum.Util;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 以太坊地址验证器
    /// Ethereum address validator
    /// </summary>
    public class EthereumAddressValidator : IAddressValidator
    {
        private readonly AddressUtil _addressUtil;
        private static readonly Regex _addressRegex = new Regex("^(0x)?[0-9a-fA-F]{40}$");

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public EthereumAddressValidator()
        {
            _addressUtil = new AddressUtil();
        }

        /// <summary>
        /// 验证以太坊地址
        /// Validate Ethereum address
        /// </summary>
        /// <param name="address">要验证的地址 / Address to validate</param>
        /// <returns>验证结果 / Validation result</returns>
        public async Task<AddressValidationResult> ValidateAddress(string address)
        {
            var result = new AddressValidationResult
            {
                IsValid = false,
                Network = "Ethereum",
                AddressType = "EOA" // 外部拥有账户 / Externally Owned Account
            };

            try
            {
                // 检查基本格式
                // Check basic format
                if (!_addressRegex.IsMatch(address))
                {
                    result.ErrorMessage = "地址格式无效 / Invalid address format";
                    return result;
                }

                // 确保地址以0x开头
                // Ensure address starts with 0x
                if (!address.StartsWith("0x"))
                {
                    address = "0x" + address;
                }

                // 验证校验和
                // Validate checksum
                if (_addressUtil.IsChecksumAddress(address))
                {
                    result.IsValid = true;
                    result.AddressType = "EOA (Checksum)";
                }
                else if (_addressUtil.IsValidEthereumAddressHexFormat(address))
                {
                    result.IsValid = true;
                }
                else
                {
                    result.ErrorMessage = "地址校验和无效 / Invalid address checksum";
                }

                // 检查是否为合约地址
                // Check if it's a contract address
                if (result.IsValid && await IsContractAddressAsync(address))
                {
                    result.AddressType = "Contract";
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"地址验证失败 / Address validation failed: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 检查是否为合约地址（简化版）
        /// Check if address is a contract address (simplified version)
        /// </summary>
        /// <param name="address">要检查的地址 / Address to check</param>
        /// <returns>是否为合约地址 / Whether it's a contract address</returns>
        private async Task<bool> IsContractAddressAsync(string address)
        {
            // 这里可以通过web3调用来检查地址是否有代码
            // Here we can use web3 call to check if address has code
            // 简化版本中，我们只检查一些常见的合约地址特征
            // In simplified version, we only check some common contract address patterns
            
            // 例如，USDT合约地址
            // For example, USDT contract address
            if (address.Equals("0xdac17f958d2ee523a2206206994597c13d831ec7", StringComparison.OrdinalIgnoreCase))
                return true;

            try
            {
                var web3 = new Nethereum.Web3.Web3("https://mainnet.infura.io/v3/"); // 可以从配置中获取 / Can be obtained from configuration
                var code = await web3.Eth.GetCode.SendRequestAsync(address);
                return code != "0x" && code != "0x0";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 生成校验和地址
        /// Generate checksum address
        /// </summary>
        /// <param name="address">原始地址 / Original address</param>
        /// <returns>带校验和的地址 / Address with checksum</returns>
        public string ToChecksumAddress(string address)
        {
            return _addressUtil.ConvertToChecksumAddress(address);
        }
    }
}
