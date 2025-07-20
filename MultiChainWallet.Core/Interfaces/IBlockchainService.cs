using System;
using System.Threading.Tasks;

namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// 区块链服务接口
    /// Blockchain service interface
    /// </summary>
    public interface IBlockchainService
    {
        /// <summary>
        /// 估算Gas费用
        /// Estimate gas fee for a contract call
        /// </summary>
        /// <param name="contractAddress">合约地址 / Contract address</param>
        /// <param name="methodName">方法名称 / Method name</param>
        /// <param name="parameters">方法参数 / Method parameters</param>
        /// <returns>预估的Gas费用 / Estimated gas fee</returns>
        Task<decimal> EstimateGasFee(string contractAddress, string methodName, string[] parameters);

        /// <summary>
        /// 获取当前Gas价格
        /// Get current gas price
        /// </summary>
        /// <returns>当前Gas价格 / Current gas price</returns>
        Task<decimal> GetGasPrice();

        /// <summary>
        /// 获取当前区块号
        /// Get current block number
        /// </summary>
        /// <returns>当前区块号 / Current block number</returns>
        Task<long> GetBlockNumber();
    }
}
