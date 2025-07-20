using System;
using System.Threading.Tasks;
using MultiChainWallet.Core.Interfaces;
using NBitcoin;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 地址验证器实现
    /// Address validator implementation
    /// </summary>
    public class AddressValidator : IAddressValidator
    {
        /// <summary>
        /// 验证地址
        /// Validate address
        /// </summary>
        public async Task<AddressValidationResult> ValidateAddress(string address)
        {
            var result = new AddressValidationResult
            {
                IsValid = false,
                AddressType = "Unknown",
                Network = "Unknown",
                ErrorMessage = string.Empty
            };

            try
            {
                // Try to parse as Bitcoin address
                try
                {
                    var bitcoinAddress = BitcoinAddress.Create(address, Network.Main);
                    result.IsValid = true;
                    result.AddressType = "Bitcoin";
                    result.Network = bitcoinAddress.Network.Name;
                    return result;
                }
                catch
                {
                    // Not a valid Bitcoin address, try Ethereum
                }

                // Try to parse as Ethereum address
                if (address.StartsWith("0x") && address.Length == 42)
                {
                    // Simple Ethereum address format validation
                    // For production, use a more robust validation
                    result.IsValid = true;
                    result.AddressType = "Ethereum";
                    result.Network = "Mainnet"; // Or determine from network configuration
                    return result;
                }

                result.ErrorMessage = "Invalid address format";
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                return result;
            }
        }
    }
}
