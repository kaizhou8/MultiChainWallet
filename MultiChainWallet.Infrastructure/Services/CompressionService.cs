using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 压缩服务类
    /// Compression service class
    /// </summary>
    public class CompressionService
    {
        /// <summary>
        /// 压缩级别
        /// Compression level
        /// </summary>
        private readonly CompressionLevel _compressionLevel;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public CompressionService(CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            _compressionLevel = compressionLevel;
        }

        /// <summary>
        /// 压缩数据
        /// Compress data
        /// </summary>
        public async Task<byte[]> CompressAsync(byte[] data)
        {
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(outputStream, _compressionLevel))
            {
                await gzipStream.WriteAsync(data, 0, data.Length);
            }
            return outputStream.ToArray();
        }

        /// <summary>
        /// 解压缩数据
        /// Decompress data
        /// </summary>
        public async Task<byte[]> DecompressAsync(byte[] compressedData)
        {
            using var inputStream = new MemoryStream(compressedData);
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            {
                await gzipStream.CopyToAsync(outputStream);
            }
            return outputStream.ToArray();
        }

        /// <summary>
        /// 压缩文件
        /// Compress file
        /// </summary>
        public async Task CompressFileAsync(string sourcePath, string destinationPath)
        {
            using var sourceStream = File.OpenRead(sourcePath);
            using var destinationStream = File.Create(destinationPath);
            using var gzipStream = new GZipStream(destinationStream, _compressionLevel);
            await sourceStream.CopyToAsync(gzipStream);
        }

        /// <summary>
        /// 解压缩文件
        /// Decompress file
        /// </summary>
        public async Task DecompressFileAsync(string sourcePath, string destinationPath)
        {
            using var sourceStream = File.OpenRead(sourcePath);
            using var destinationStream = File.Create(destinationPath);
            using var gzipStream = new GZipStream(sourceStream, CompressionMode.Decompress);
            await gzipStream.CopyToAsync(destinationStream);
        }

        /// <summary>
        /// 获取压缩比率
        /// Get compression ratio
        /// </summary>
        public double GetCompressionRatio(long originalSize, long compressedSize)
        {
            if (originalSize == 0)
            {
                return 0;
            }
            return 1 - ((double)compressedSize / originalSize);
        }

        /// <summary>
        /// 估计压缩后的大小
        /// Estimate compressed size
        /// </summary>
        public async Task<long> EstimateCompressedSizeAsync(byte[] data)
        {
            var compressedData = await CompressAsync(data);
            return compressedData.Length;
        }

        /// <summary>
        /// 检查文件是否已压缩
        /// Check if file is compressed
        /// </summary>
        public bool IsFileCompressed(string filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
                // 尝试读取一个字节来验证是否为有效的GZip文件
                // Try to read one byte to verify if it's a valid GZip file
                gzipStream.ReadByte();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
