using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Interfaces;
using MultiChainWallet.Core.Models;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 文件市场数据缓存服务实现
    /// File-based market data cache service implementation
    /// </summary>
    public class FileMarketDataCacheService : IMarketDataCacheService
    {
        private readonly string _cacheDirectory;
        private readonly ILogger<FileMarketDataCacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private int _cacheExpirationMinutes = 15;
        private readonly object _expirationLock = new object();

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public FileMarketDataCacheService(ILogger<FileMarketDataCacheService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "MultiChainWallet", "MarketDataCache");
            
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
                _logger.LogInformation("Created market data cache directory at {Directory}", _cacheDirectory);
            }

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            _logger.LogInformation("FileMarketDataCacheService initialized");
        }

        /// <summary>
        /// 保存币种价格
        /// Save coin price
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <param name="vsCurrency">计价货币 / Vs currency</param>
        /// <param name="priceInfo">价格信息 / Price information</param>
        public async Task SaveCoinPriceAsync(string symbol, string vsCurrency, CryptoPriceInfo priceInfo)
        {
            if (priceInfo == null)
            {
                throw new ArgumentNullException(nameof(priceInfo));
            }

            string cacheKey = GetPriceCacheKey(symbol, vsCurrency);
            string filePath = GetCacheFilePath(cacheKey);
            
            try
            {
                var cacheItem = new CacheItem<CryptoPriceInfo>
                {
                    Data = priceInfo,
                    CachedAt = DateTime.UtcNow,
                    ExpirationMinutes = GetCacheExpirationMinutes()
                };

                string json = JsonSerializer.Serialize(cacheItem, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                _logger.LogDebug("Saved price cache for {Symbol} in {Currency}", symbol, vsCurrency);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving price cache for {Symbol}: {Message}", symbol, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 获取缓存的币种价格
        /// Get cached coin price
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <param name="vsCurrency">计价货币 / Vs currency</param>
        /// <returns>价格信息，如果没有缓存则返回null / Price information, or null if not cached</returns>
        public async Task<CryptoPriceInfo> GetCachedCoinPriceAsync(string symbol, string vsCurrency)
        {
            string cacheKey = GetPriceCacheKey(symbol, vsCurrency);
            string filePath = GetCacheFilePath(cacheKey);

            if (!File.Exists(filePath))
            {
                _logger.LogDebug("No price cache found for {Symbol} in {Currency}", symbol, vsCurrency);
                return null;
            }

            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                var cacheItem = JsonSerializer.Deserialize<CacheItem<CryptoPriceInfo>>(json, _jsonOptions);
                
                if (cacheItem != null && cacheItem.Data != null)
                {
                    _logger.LogDebug("Retrieved price cache for {Symbol} in {Currency}", symbol, vsCurrency);
                    return cacheItem.Data;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving price cache for {Symbol}: {Message}", symbol, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 缓存是否有效
        /// Whether the cache is valid
        /// </summary>
        /// <param name="cacheDate">缓存日期 / Cache date</param>
        /// <returns>缓存是否有效 / Whether cache is valid</returns>
        public bool IsCacheValid(DateTime cacheDate)
        {
            TimeSpan expirationTime = TimeSpan.FromMinutes(GetCacheExpirationMinutes());
            DateTime now = DateTime.UtcNow;
            TimeSpan age = now - cacheDate;
            
            return age <= expirationTime;
        }

        /// <summary>
        /// 保存历史价格数据
        /// Save historical price data
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <param name="vsCurrency">计价货币 / Vs currency</param>
        /// <param name="days">天数 / Number of days</param>
        /// <param name="dataPoints">历史价格数据点 / Historical price data points</param>
        public async Task SaveHistoricalDataAsync(string symbol, string vsCurrency, int days, List<PriceDataPoint> dataPoints)
        {
            if (dataPoints == null)
            {
                throw new ArgumentNullException(nameof(dataPoints));
            }

            string interval = GetIntervalForDays(days);
            string cacheKey = GetHistoricalCacheKey(symbol, vsCurrency, days, interval);
            string filePath = GetCacheFilePath(cacheKey);
            
            try
            {
                var cacheItem = new CacheItem<List<PriceDataPoint>>
                {
                    Data = dataPoints,
                    CachedAt = DateTime.UtcNow,
                    ExpirationMinutes = 360 // 6小时 / 6 hours
                };

                string json = JsonSerializer.Serialize(cacheItem, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                _logger.LogDebug("Saved historical data cache for {Symbol} in {Currency} for {Days} days", symbol, vsCurrency, days);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving historical data cache for {Symbol}: {Message}", symbol, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 获取缓存的历史价格数据
        /// Get cached historical price data
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <param name="vsCurrency">计价货币 / Vs currency</param>
        /// <param name="days">天数 / Number of days</param>
        /// <returns>历史价格数据点，如果没有缓存则返回null / Historical price data points, or null if not cached</returns>
        public async Task<List<PriceDataPoint>> GetCachedHistoricalDataAsync(string symbol, string vsCurrency, int days)
        {
            string interval = GetIntervalForDays(days);
            string cacheKey = GetHistoricalCacheKey(symbol, vsCurrency, days, interval);
            string filePath = GetCacheFilePath(cacheKey);

            if (!File.Exists(filePath))
            {
                _logger.LogDebug("No historical data cache found for {Symbol} in {Currency} for {Days} days", symbol, vsCurrency, days);
                return null;
            }

            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                var cacheItem = JsonSerializer.Deserialize<CacheItem<List<PriceDataPoint>>>(json, _jsonOptions);
                
                if (cacheItem != null && cacheItem.Data != null)
                {
                    _logger.LogDebug("Retrieved historical data cache for {Symbol} in {Currency} for {Days} days", symbol, vsCurrency, days);
                    return cacheItem.Data;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving historical data cache for {Symbol}: {Message}", symbol, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 保存市场信息
        /// Save market information
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <param name="marketInfo">市场信息 / Market information</param>
        public async Task SaveMarketInfoAsync(string symbol, CryptoMarketInfo marketInfo)
        {
            if (marketInfo == null)
            {
                throw new ArgumentNullException(nameof(marketInfo));
            }

            string cacheKey = GetMarketInfoCacheKey(symbol);
            string filePath = GetCacheFilePath(cacheKey);
            
            try
            {
                var cacheItem = new CacheItem<CryptoMarketInfo>
                {
                    Data = marketInfo,
                    CachedAt = DateTime.UtcNow,
                    ExpirationMinutes = 360 // 6小时 / 6 hours
                };

                string json = JsonSerializer.Serialize(cacheItem, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                _logger.LogDebug("Saved market info cache for {Symbol}", symbol);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving market info cache for {Symbol}: {Message}", symbol, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 获取缓存的市场信息
        /// Get cached market information
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <returns>市场信息，如果没有缓存则返回null / Market information, or null if not cached</returns>
        public async Task<CryptoMarketInfo> GetCachedMarketInfoAsync(string symbol)
        {
            string cacheKey = GetMarketInfoCacheKey(symbol);
            string filePath = GetCacheFilePath(cacheKey);

            if (!File.Exists(filePath))
            {
                _logger.LogDebug("No market info cache found for {Symbol}", symbol);
                return null;
            }

            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                var cacheItem = JsonSerializer.Deserialize<CacheItem<CryptoMarketInfo>>(json, _jsonOptions);
                
                if (cacheItem != null && cacheItem.Data != null)
                {
                    _logger.LogDebug("Retrieved market info cache for {Symbol}", symbol);
                    return cacheItem.Data;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving market info cache for {Symbol}: {Message}", symbol, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 保存热门币种
        /// Save trending coins
        /// </summary>
        /// <param name="trendingCoins">热门币种列表 / List of trending coins</param>
        public async Task SaveTrendingCoinsAsync(List<CryptoListItem> trendingCoins)
        {
            if (trendingCoins == null)
            {
                throw new ArgumentNullException(nameof(trendingCoins));
            }

            string cacheKey = "trending_coins";
            string filePath = GetCacheFilePath(cacheKey);
            
            try
            {
                var cacheItem = new CacheItem<List<CryptoListItem>>
                {
                    Data = trendingCoins,
                    CachedAt = DateTime.UtcNow,
                    ExpirationMinutes = 360 // 6小时 / 6 hours
                };

                string json = JsonSerializer.Serialize(cacheItem, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                _logger.LogDebug("Saved trending coins cache");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving trending coins cache: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 获取缓存的热门币种
        /// Get cached trending coins
        /// </summary>
        /// <returns>热门币种列表，如果没有缓存则返回null / List of trending coins, or null if not cached</returns>
        public async Task<List<CryptoListItem>> GetCachedTrendingCoinsAsync()
        {
            string cacheKey = "trending_coins";
            string filePath = GetCacheFilePath(cacheKey);

            if (!File.Exists(filePath))
            {
                _logger.LogDebug("No trending coins cache found");
                return null;
            }

            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                var cacheItem = JsonSerializer.Deserialize<CacheItem<List<CryptoListItem>>>(json, _jsonOptions);
                
                if (cacheItem != null && cacheItem.Data != null)
                {
                    _logger.LogDebug("Retrieved trending coins cache");
                    return cacheItem.Data;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trending coins cache: {Message}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 清除缓存
        /// Clear cache
        /// </summary>
        public async Task ClearCacheAsync()
        {
            try
            {
                // 异步调用Task.Run执行文件删除
                // Call Task.Run to delete files asynchronously
                await Task.Run(() =>
                {
                    try
                    {
                        foreach (var file in Directory.GetFiles(_cacheDirectory))
                        {
                            File.Delete(file);
                        }
                        _logger.LogInformation("Cleared all market data cache files");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error clearing cache files: {Message}", ex.Message);
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ClearCacheAsync: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 设置缓存过期时间
        /// Set cache expiration time
        /// </summary>
        /// <param name="expirationTime">过期时间（分钟） / Expiration time (minutes)</param>
        public void SetCacheExpirationTime(int expirationTime)
        {
            if (expirationTime <= 0)
            {
                throw new ArgumentException("Expiration time must be greater than zero", nameof(expirationTime));
            }

            lock (_expirationLock)
            {
                _cacheExpirationMinutes = expirationTime;
                _logger.LogInformation("Cache expiration time set to {Minutes} minutes", expirationTime);
            }
        }

        /// <summary>
        /// 获取缓存过期时间（分钟）
        /// Get cache expiration time in minutes
        /// </summary>
        /// <returns>缓存过期时间（分钟） / Cache expiration time in minutes</returns>
        private int GetCacheExpirationMinutes()
        {
            lock (_expirationLock)
            {
                return _cacheExpirationMinutes;
            }
        }

        /// <summary>
        /// 获取缓存文件路径
        /// Get cache file path
        /// </summary>
        /// <param name="cacheKey">缓存键 / Cache key</param>
        /// <returns>文件路径 / File path</returns>
        private string GetCacheFilePath(string cacheKey)
        {
            return Path.Combine(_cacheDirectory, $"{cacheKey}.json");
        }

        /// <summary>
        /// 获取价格缓存键
        /// Get price cache key
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <param name="currency">计价货币 / Currency</param>
        /// <returns>缓存键 / Cache key</returns>
        private string GetPriceCacheKey(string symbol, string currency)
        {
            return $"price_{symbol.ToLowerInvariant()}_{currency.ToLowerInvariant()}";
        }

        /// <summary>
        /// 获取历史数据缓存键
        /// Get historical data cache key
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <param name="currency">计价货币 / Currency</param>
        /// <param name="days">天数 / Days</param>
        /// <param name="interval">间隔 / Interval</param>
        /// <returns>缓存键 / Cache key</returns>
        private string GetHistoricalCacheKey(string symbol, string currency, int days, string interval)
        {
            return $"history_{symbol.ToLowerInvariant()}_{currency.ToLowerInvariant()}_{days}_{interval}";
        }

        /// <summary>
        /// 获取市场信息缓存键
        /// Get market info cache key
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <returns>缓存键 / Cache key</returns>
        private string GetMarketInfoCacheKey(string symbol)
        {
            return $"market_{symbol.ToLowerInvariant()}";
        }

        /// <summary>
        /// 获取适合天数的间隔
        /// Get interval suitable for days
        /// </summary>
        /// <param name="days">天数 / Number of days</param>
        /// <returns>间隔 / Interval</returns>
        private string GetIntervalForDays(int days)
        {
            if (days <= 1) return "5minute";
            if (days <= 7) return "1hour";
            if (days <= 30) return "4hour";
            if (days <= 90) return "1day";
            return "1day";
        }

        /// <summary>
        /// 缓存项类
        /// Cache item class
        /// </summary>
        /// <typeparam name="T">数据类型 / Data type</typeparam>
        private class CacheItem<T>
        {
            /// <summary>
            /// 缓存数据
            /// Cached data
            /// </summary>
            public T Data { get; set; }

            /// <summary>
            /// 缓存时间
            /// Cache time
            /// </summary>
            public DateTime CachedAt { get; set; }

            /// <summary>
            /// 过期时间（分钟）
            /// Expiration time (minutes)
            /// </summary>
            public int ExpirationMinutes { get; set; }
        }
    }
} 