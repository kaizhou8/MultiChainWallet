using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using MultiChainWallet.Infrastructure.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MultiChainWallet.Tests.Services
{
    /// <summary>
    /// 压缩服务测试类
    /// Compression service test class
    /// </summary>
    [TestClass]
    public class CompressionServiceTests
    {
        private CompressionService _compressionService;
        private string _testDataDirectory;

        [TestInitialize]
        public void Initialize()
        {
            _compressionService = new CompressionService(CompressionLevel.Optimal);
            _testDataDirectory = Path.Combine(Path.GetTempPath(), "MultiChainWalletTests");
            Directory.CreateDirectory(_testDataDirectory);
        }

        [TestMethod]
        public async Task CompressAndDecompress_ShouldReturnOriginalData()
        {
            // Arrange
            var originalData = Encoding.UTF8.GetBytes("Test data for compression");

            // Act
            var compressedData = await _compressionService.CompressAsync(originalData);
            var decompressedData = await _compressionService.DecompressAsync(compressedData);

            // Assert
            Assert.AreEqual(originalData, decompressedData);
        }

        [TestMethod]
        public async Task CompressAndDecompress_LargeData_ShouldReturnOriginalData()
        {
            // Arrange
            var originalData = new byte[1024 * 1024]; // 1MB of data
            new Random().NextBytes(originalData);

            // Act
            var compressedData = await _compressionService.CompressAsync(originalData);
            var decompressedData = await _compressionService.DecompressAsync(compressedData);

            // Assert
            Assert.AreEqual(originalData, decompressedData);
        }

        [TestMethod]
        public async Task CompressFile_ShouldCreateCompressedFile()
        {
            // Arrange
            var sourceFile = Path.Combine(_testDataDirectory, "source.txt");
            var compressedFile = Path.Combine(_testDataDirectory, "compressed.gz");
            var originalContent = "Test content for file compression";
            await File.WriteAllTextAsync(sourceFile, originalContent);

            // Act
            await _compressionService.CompressFileAsync(sourceFile, compressedFile);

            // Assert
            Assert.IsTrue(File.Exists(compressedFile));
            Assert.IsTrue(_compressionService.IsFileCompressed(compressedFile));
        }

        [TestMethod]
        public async Task CompressAndDecompressFile_ShouldReturnOriginalContent()
        {
            // Arrange
            var sourceFile = Path.Combine(_testDataDirectory, "source2.txt");
            var compressedFile = Path.Combine(_testDataDirectory, "compressed2.gz");
            var decompressedFile = Path.Combine(_testDataDirectory, "decompressed.txt");
            var originalContent = "Test content for file compression and decompression";
            await File.WriteAllTextAsync(sourceFile, originalContent);

            // Act
            await _compressionService.CompressFileAsync(sourceFile, compressedFile);
            await _compressionService.DecompressFileAsync(compressedFile, decompressedFile);

            // Assert
            var decompressedContent = await File.ReadAllTextAsync(decompressedFile);
            Assert.AreEqual(originalContent, decompressedContent);
        }

        [TestMethod]
        public async Task GetCompressionRatio_ShouldReturnExpectedRatio()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes(new string('a', 1000)); // Highly compressible data

            // Act
            var compressedData = await _compressionService.CompressAsync(data);
            var ratio = _compressionService.GetCompressionRatio(data.Length, compressedData.Length);

            // Assert
            Assert.IsTrue(ratio > 0);
            Assert.IsTrue(ratio < 1);
        }

        [TestMethod]
        public void GetCompressionRatio_WithZeroOriginalSize_ShouldReturnZero()
        {
            // Act
            var ratio = _compressionService.GetCompressionRatio(0, 100);

            // Assert
            Assert.AreEqual(0, ratio);
        }

        [TestMethod]
        public async Task EstimateCompressedSize_ShouldReturnReasonableEstimate()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes(new string('a', 1000));

            // Act
            var estimatedSize = await _compressionService.EstimateCompressedSizeAsync(data);

            // Assert
            Assert.IsTrue(estimatedSize > 0);
            Assert.IsTrue(estimatedSize < data.Length);
        }

        [TestMethod]
        public void IsFileCompressed_WithNonCompressedFile_ShouldReturnFalse()
        {
            // Arrange
            var nonCompressedFile = Path.Combine(_testDataDirectory, "noncompressed.txt");
            File.WriteAllText(nonCompressedFile, "Non-compressed content");

            // Act
            var isCompressed = _compressionService.IsFileCompressed(nonCompressedFile);

            // Assert
            Assert.IsFalse(isCompressed);
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                Directory.Delete(_testDataDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
