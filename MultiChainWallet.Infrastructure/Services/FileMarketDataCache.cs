using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Interfaces;
using MultiChainWallet.Core.Models;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 基于文件的市场数据缓存实现
    /// File-based market data cache implementation
    /// </summary>
    public class FileMarketDataCache : IMarketDataCache
    {
        private readonly string _cacheDirectory;
        private readonly ILogger<FileMarketDataCache> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="logger">日志器 / Logger</param>
        public FileMarketDataCache(ILogger<FileMarketDataCache> logger)
        {
            _logger = logger;
            
            // 设置缓存目录
            // Set cache directory
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _cacheDirectory = Path.Combine(appDataPath, "MultiChainWallet", "MarketDataCache");
            
            // 确保缓存目录存在
            // Ensure cache directory exists
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }
            
            // 配置JSON序列化选项
            // Configure JSON serialization options
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            _logger.LogInformation("市场数据缓存初始化，缓存目录: {CacheDirectory} / Market data cache initialized, cache directory: {CacheDirectory}", _cacheDirectory);
        }

        /// <summary>
        /// 获取价格缓存
        /// Get price cache
        /// </summary>
        public async Task<CryptoPriceInfo> GetPriceCacheAsync(string symbol, string currency)
        {
            string cacheKey = GetPriceCacheKey(symbol, currency);
            return await GetCacheItemAsync<CryptoPriceInfo>(cacheKey);
        }

        /// <summary>
        /// 设置价格缓存
        /// Set price cache
        /// </summary>
        public async Task SetPriceCacheAsync(CryptoPriceInfo priceInfo, int expirationMinutes = 15)
        {
            if (priceInfo == null)
            {
                throw new ArgumentNullException(nameof(priceInfo));
            }

            string cacheKey = GetPriceCacheKey(priceInfo.Symbol, priceInfo.Currency);
            await SetCacheItemAsync(cacheKey, priceInfo, expirationMinutes);
        }

        /// <summary>
        /// 获取批量价格缓存
        /// Get bulk price cache
        /// </summary>
        public async Task<Dictionary<string, CryptoPriceInfo>> GetBulkPriceCacheAsync(IEnumerable<string> symbols, string currency)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            var result = new Dictionary<string, CryptoPriceInfo>();
            
            foreach (var symbol in symbols)
            {
                var priceInfo = await GetPriceCacheAsync(symbol, currency);
                if (priceInfo != null)
                {
                    result[symbol] = priceInfo;
                }
            }
            
            return result;
        }

        /// <summary>
        /// 设置批量价格缓存
        /// Set bulk price cache
        /// </summary>
        public async Task SetBulkPriceCacheAsync(Dictionary<string, CryptoPriceInfo> pricesInfo, int expirationMinutes = 15)
        {
            if (pricesInfo == null)
            {
                throw new ArgumentNullException(nameof(pricesInfo));
            }

            foreach (var kvp in pricesInfo)
            {
                await SetPriceCacheAsync(kvp.Value, expirationMinutes);
            }
        }

        /// <summary>
        /// 获取历史数据缓存
        /// Get historical data cache
        /// </summary>
        public async Task<List<PriceDataPoint>> GetHistoricalDataCacheAsync(string symbol, string currency, int days, string interval)
        {
            string cacheKey = GetHistoricalDataCacheKey(symbol, currency, days, interval);
            return await GetCacheItemAsync<List<PriceDataPoint>>(cacheKey);
        }

        /// <summary>
        /// 设置历史数据缓存
        /// Set historical data cache
        /// </summary>
        public async Task SetHistoricalDataCacheAsync(string symbol, string currency, int days, string interval, List<PriceDataPoint> dataPoints, int expirationHours = 6)
        {
            if (dataPoints == null)
            {
                throw new ArgumentNullException(nameof(dataPoints));
            }

            string cacheKey = GetHistoricalDataCacheKey(symbol, currency, days, interval);
            await SetCacheItemAsync(cacheKey, dataPoints, expirationHours * 60);
        }

        /// <summary>
        /// 获取市场信息缓存
        /// Get market info cache
        /// </summary>
        public async Task<CryptoMarketInfo> GetMarketInfoCacheAsync(string symbol)
        {
            string cacheKey = GetMarketInfoCacheKey(symbol);
            return await GetCacheItemAsync<CryptoMarketInfo>(cacheKey);
        }

        /// <summary>
        /// 设置市场信息缓存
        /// Set market info cache
        /// </summary>
        public async Task SetMarketInfoCacheAsync(CryptoMarketInfo marketInfo, int expirationHours = 6)
        {
            if (marketInfo == null)
            {
                throw new ArgumentNullException(nameof(marketInfo));
            }

            string cacheKey = GetMarketInfoCacheKey(marketInfo.Symbol);
            await SetCacheItemAsync(cacheKey, marketInfo, expirationHours * 60);
        }

        /// <summary>
        /// 获取热门币种缓存
        /// Get trending coins cache
        /// </summary>
        public async Task<List<CryptoListItem>> GetTrendingCoinsCacheAsync()
        {
            string cacheKey = "trending_coins";
            return await GetCacheItemAsync<List<CryptoListItem>>(cacheKey);
        }

        /// <summary>
        /// 设置热门币种缓存
        /// Set trending coins cache
        /// </summary>
        public async Task SetTrendingCoinsCacheAsync(List<CryptoListItem> trendingCoins, int expirationHours = 6)
        {
            if (trendingCoins == null)
            {
                throw new ArgumentNullException(nameof(trendingCoins));
            }

            string cacheKey = "trending_coins";
            await SetCacheItemAsync(cacheKey, trendingCoins, expirationHours * 60);
        }

        /// <summary>
        /// 清除特定币种的所有缓存
        /// Clear all caches for a specific coin
        /// </summary>
        public async Task ClearCoinCacheAsync(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            try
            {
                var files = Directory.GetFiles(_cacheDirectory, $"{symbol.ToLower()}_*.json");
                foreach (var file in files)
                {
                    File.Delete(file);
                }
                
                _logger.LogInformation("已清除币种 {Symbol} 的所有缓存 / Cleared all caches for coin {Symbol}", symbol);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除币种 {Symbol} 缓存时出错 / Error clearing cache for coin {Symbol}", symbol);
                throw;
            }
        }

        /// <summary>
        /// 清除所有缓存
        /// Clear all caches
        /// </summary>
        public async Task ClearAllCacheAsync()
        {
            try
            {
                var files = Directory.GetFiles(_cacheDirectory, "*.json");
                foreach (var file in files)
                {
                    File.Delete(file);
                }
                
                _logger.LogInformation("已清除所有市场数据缓存 / Cleared all market data caches");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除所有市场数据缓存时出错 / Error clearing all market data caches");
                throw;
            }
        }

        /// <summary>
        /// 缓存是否过期
        /// Check if cache is expired
        /// </summary>
        public async Task<bool> IsCacheExpiredAsync(string cacheKey)
        {
            string filePath = Path.Combine(_cacheDirectory, $"{cacheKey}.json");
            if (!File.Exists(filePath))
            {
                return true;
            }

            try
            {
                string metaFilePath = Path.Combine(_cacheDirectory, $"{cacheKey}.meta");
                if (!File.Exists(metaFilePath))
                {
                    return true;
                }

                string metaJson = await File.ReadAllTextAsync(metaFilePath);
                var metadata = JsonSerializer.Deserialize<CacheMetadata>(metaJson, _jsonOptions);
                
                return metadata?.ExpirationTime < DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查缓存 {CacheKey} 是否过期时出错 / Error checking if cache {CacheKey} is expired", cacheKey);
                return true;
            }
        }

        // 私有辅助方法 / Private helper methods

        private string GetPriceCacheKey(string symbol, string currency)
        {
            return $"{symbol.ToLower()}_{currency.ToLower()}_price";
        }

        private string GetHistoricalDataCacheKey(string symbol, string currency, int days, string interval)
        {
            return $"{symbol.ToLower()}_{currency.ToLower()}_history_{days}_{interval}";
        }

        private string GetMarketInfoCacheKey(string symbol)
        {
            return $"{symbol.ToLower()}_market_info";
        }

        private async Task<T> GetCacheItemAsync<T>(string cacheKey) where T : class
        {
            if (await IsCacheExpiredAsync(cacheKey))
            {
                return null;
            }

            try
            {
                string filePath = Path.Combine(_cacheDirectory, $"{cacheKey}.json");
                if (!File.Exists(filePath))
                {
                    return null;
                }

                string json = await File.ReadAllTextAsync(filePath);
                var result = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                
                _logger.LogDebug("从缓存获取数据 {CacheKey} / Retrieved data from cache {CacheKey}", cacheKey);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从缓存获取数据 {CacheKey} 时出错 / Error retrieving data from cache {CacheKey}", cacheKey);
                return null;
            }
        }

        private async Task SetCacheItemAsync<T>(string cacheKey, T item, int expirationMinutes) where T : class
        {
            try
            {
                // 保存数据
                // Save data
                string filePath = Path.Combine(_cacheDirectory, $"{cacheKey}.json");
                string json = JsonSerializer.Serialize(item, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                
                // 保存元数据（过期时间等）
                // Save metadata (expiration time, etc.)
                string metaFilePath = Path.Combine(_cacheDirectory, $"{cacheKey}.meta");
                var metadata = new CacheMetadata
                {
                    CreationTime = DateTime.UtcNow,
                    ExpirationTime = DateTime.UtcNow.AddMinutes(expirationMinutes)
                };
                string metaJson = JsonSerializer.Serialize(metadata, _jsonOptions);
                await File.WriteAllTextAsync(metaFilePath, metaJson);
                
                _logger.LogDebug("已缓存数据 {CacheKey}，过期时间为 {ExpirationMinutes} 分钟 / Cached data {CacheKey} with expiration time of {ExpirationMinutes} minutes", 
                    cacheKey, expirationMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "缓存数据 {CacheKey} 时出错 / Error caching data {CacheKey}", cacheKey);
                throw;
            }
        }

        /// <summary>
        /// 缓存元数据
        /// Cache metadata
        /// </summary>
        private class CacheMetadata
        {
            /// <summary>
            /// 创建时间
            /// Creation time
            /// </summary>
            public DateTime CreationTime { get; set; }
            
            /// <summary>
            /// 过期时间
            /// Expiration time
            /// </summary>
            public DateTime ExpirationTime { get; set; }
        }
    }
} 