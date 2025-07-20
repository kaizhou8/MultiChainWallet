using System;
using System.Threading.Tasks;
using MultiChainWallet.Core.Interfaces;
using Nethereum.Web3;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Numerics;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 区块链服务实现
    /// Blockchain service implementation
    /// </summary>
    public class BlockchainService : IBlockchainService
    {
        private readonly Web3 _web3;
        private const string ERC20_ABI = @"[{""constant"":true,""inputs"":[],""name"":""name"",""outputs"":[{""name"":"""",""type"":""string""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_spender"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""approve"",""outputs"":[{""name"":"""",""type"":""bool""}],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""constant"":true,""inputs"":[],""name"":""totalSupply"",""outputs"":[{""name"":"""",""type"":""uint256""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_from"",""type"":""address""},{""name"":""_to"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""transferFrom"",""outputs"":[{""name"":"""",""type"":""bool""}],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""constant"":true,""inputs"":[{""name"":""_owner"",""type"":""address""}],""name"":""balanceOf"",""outputs"":[{""name"":""balance"",""type"":""uint256""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":true,""inputs"":[],""name"":""symbol"",""outputs"":[{""name"":"""",""type"":""string""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_to"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""transfer"",""outputs"":[{""name"":"""",""type"":""bool""}],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""constant"":true,""inputs"":[{""name"":""_owner"",""type"":""address""},{""name"":""_spender"",""type"":""address""}],""name"":""allowance"",""outputs"":[{""name"":"""",""type"":""uint256""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""anonymous"":false,""inputs"":[{""indexed"":true,""name"":""owner"",""type"":""address""},{""indexed"":true,""name"":""spender"",""type"":""address""},{""indexed"":false,""name"":""value"",""type"":""uint256""}],""name"":""Approval"",""type"":""event""},{""anonymous"":false,""inputs"":[{""indexed"":true,""name"":""from"",""type"":""address""},{""indexed"":true,""name"":""to"",""type"":""address""},{""indexed"":false,""name"":""value"",""type"":""uint256""}],""name"":""Transfer"",""type"":""event""}]";

        public BlockchainService(Web3 web3)
        {
            _web3 = web3;
        }

        /// <summary>
        /// 估算Gas费用
        /// Estimate gas fee for a contract call
        /// </summary>
        public async Task<decimal> EstimateGasFee(string contractAddress, string methodName, string[] parameters)
        {
            try
            {
                var contract = _web3.Eth.GetContract(ERC20_ABI, contractAddress);
                var function = contract.GetFunction(methodName);

                var gas = await function.EstimateGasAsync(
                    parameters
                );

                var gasPrice = await GetGasPrice();
                var gasPriceInWei = Web3.Convert.ToWei(gasPrice);
                return Web3.Convert.FromWei(gas.Value * gasPriceInWei);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to estimate gas fee: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取当前Gas价格
        /// Get current gas price
        /// </summary>
        public async Task<decimal> GetGasPrice()
        {
            try
            {
                var gasPrice = await _web3.Eth.GasPrice.SendRequestAsync();
                return Web3.Convert.FromWei(gasPrice);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get gas price: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取当前区块号
        /// Get current block number
        /// </summary>
        public async Task<long> GetBlockNumber()
        {
            try
            {
                var blockNumber = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                return (long)blockNumber.Value;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get block number: {ex.Message}");
            }
        }
    }
}
