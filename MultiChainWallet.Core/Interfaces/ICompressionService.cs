using System.Threading.Tasks;

namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// 压缩服务接口 / Compression service interface
    /// </summary>
    public interface ICompressionService
    {
        /// <summary>
        /// 压缩数据 / Compress data
        /// </summary>
        Task<byte[]> CompressAsync(byte[] data);

        /// <summary>
        /// 解压数据 / Decompress data
        /// </summary>
        Task<byte[]> DecompressAsync(byte[] compressedData);

        /// <summary>
        /// 计算压缩比率 / Calculate compression ratio
        /// </summary>
        double CalculateCompressionRatio(byte[] originalData, byte[] compressedData);
    }
}
