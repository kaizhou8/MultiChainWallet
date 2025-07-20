using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Interfaces;
using MultiChainWallet.Core.Models;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// 市场数据服务实现
    /// Market data service implementation
    /// </summary>
    public class MarketDataService : IMarketDataService
    {
        private readonly CoinGeckoApiClient _apiClient;
        private readonly IMarketDataCache _cache;
        private readonly ILogger<MarketDataService> _logger;
        private int _cacheExpirationMinutes = 15;
        private readonly object _cacheExpirationLock = new object();

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="apiClient">CoinGecko API客户端</param>
        /// <param name="cache">市场数据缓存</param>
        /// <param name="logger">日志记录器</param>
        public MarketDataService(
            CoinGeckoApiClient apiClient,
            IMarketDataCache cache,
            ILogger<MarketDataService> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 获取币种价格
        /// Get coin price
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <param name="vsCurrency">计价货币 / Vs currency</param>
        /// <param name="forceRefresh">是否强制刷新缓存 / Whether to force refresh cache</param>
        /// <returns>价格信息 / Price information</returns>
        public async Task<CryptoPriceInfo> GetCoinPriceAsync(string symbol, string vsCurrency = "usd", bool forceRefresh = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(symbol))
                {
                    throw new ArgumentException("Symbol cannot be empty", nameof(symbol));
                }

                if (string.IsNullOrWhiteSpace(vsCurrency))
                {
                    throw new ArgumentException("Currency cannot be empty", nameof(vsCurrency));
                }

                // 检查缓存
                // Check cache
                if (!forceRefresh)
                {
                    var cachedPrice = await _cache.GetPriceCacheAsync(symbol, vsCurrency);
                    if (cachedPrice != null && !await _cache.IsCacheExpiredAsync(GetCacheKey(symbol, vsCurrency)))
                    {
                        _logger.LogDebug("Retrieved {Symbol} price from cache", symbol);
                        cachedPrice.IsFromCache = true;
                        return cachedPrice;
                    }
                }

                // 从API获取
                // Get from API
                var priceInfo = await _apiClient.GetCoinPriceAsync(symbol, vsCurrency);
                if (priceInfo != null)
                {
                    priceInfo.IsFromCache = false;
                    // 存入缓存
                    // Save to cache
                    await _cache.SetPriceCacheAsync(priceInfo, _cacheExpirationMinutes);
                    _logger.LogDebug("Retrieved {Symbol} price from API and cached", symbol);
                    return priceInfo;
                }

                _logger.LogWarning("Failed to retrieve price for {Symbol}", symbol);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting price for {Symbol}: {Message}", symbol, ex.Message);
                
                // 尝试从缓存获取过期数据作为后备
                // Try to get expired data from cache as fallback
                if (!forceRefresh)
                {
                    var cachedPrice = await _cache.GetPriceCacheAsync(symbol, vsCurrency);
                    if (cachedPrice != null)
                    {
                        _logger.LogWarning("Returning expired cache data for {Symbol} as fallback", symbol);
                        cachedPrice.IsFromCache = true;
                        return cachedPrice;
                    }
                }
                
                throw;
            }
        }

        /// <summary>
        /// 获取多个币种的价格
        /// Get prices for multiple coins
        /// </summary>
        /// <param name="symbols">币种符号列表 / List of coin symbols</param>
        /// <param name="vsCurrency">计价货币 / Vs currency</param>
        /// <param name="forceRefresh">是否强制刷新缓存 / Whether to force refresh cache</param>
        /// <returns>币种到价格信息的映射 / Mapping of coin symbols to price information</returns>
        public async Task<Dictionary<string, CryptoPriceInfo>> GetCoinsPricesAsync(IEnumerable<string> symbols, string vsCurrency = "usd", bool forceRefresh = false)
        {
            if (symbols == null || !symbols.Any())
            {
                throw new ArgumentException("Symbols list cannot be empty", nameof(symbols));
            }

            if (string.IsNullOrWhiteSpace(vsCurrency))
            {
                throw new ArgumentException("Currency cannot be empty", nameof(vsCurrency));
            }

            var symbolsList = symbols.ToList();
            _logger.LogDebug("Getting prices for {Count} coins", symbolsList.Count);

            try
            {
                Dictionary<string, CryptoPriceInfo> result = new Dictionary<string, CryptoPriceInfo>(StringComparer.OrdinalIgnoreCase);
                List<string> symbolsToFetch = new List<string>();

                // 如果不强制刷新，先检查缓存
                // If not forcing refresh, check cache first
                if (!forceRefresh)
                {
                    var cachedPrices = await _cache.GetBulkPriceCacheAsync(symbolsList, vsCurrency);
                    
                    foreach (var symbol in symbolsList)
                    {
                        if (cachedPrices.TryGetValue(symbol, out var price) && 
                            !await _cache.IsCacheExpiredAsync(GetCacheKey(symbol, vsCurrency)))
                        {
                            // 使用缓存数据
                            // Use cached data
                            price.IsFromCache = true;
                            result[symbol] = price;
                        }
                        else
                        {
                            // 需要从API获取
                            // Need to fetch from API
                            symbolsToFetch.Add(symbol);
                        }
                    }
                }
                else
                {
                    // 强制刷新，所有符号都需要获取
                    // Force refresh, all symbols need to be fetched
                    symbolsToFetch.AddRange(symbolsList);
                }

                // 如果有需要从API获取的数据
                // If there is data to fetch from API
                if (symbolsToFetch.Any())
                {
                    var fetchedPrices = await _apiClient.GetCoinsPricesAsync(symbolsToFetch, vsCurrency);
                    
                    // 更新结果和缓存
                    // Update results and cache
                    Dictionary<string, CryptoPriceInfo> newCacheItems = new Dictionary<string, CryptoPriceInfo>();
                    
                    foreach (var kvp in fetchedPrices)
                    {
                        kvp.Value.IsFromCache = false;
                        result[kvp.Key] = kvp.Value;
                        newCacheItems[kvp.Key] = kvp.Value;
                    }
                    
                    // 批量更新缓存
                    // Bulk update cache
                    if (newCacheItems.Any())
                    {
                        await _cache.SetBulkPriceCacheAsync(newCacheItems, _cacheExpirationMinutes);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting prices for multiple coins: {Message}", ex.Message);
                
                // 尝试从缓存获取（包括过期数据）作为后备
                // Try to get from cache (including expired data) as fallback
                if (!forceRefresh)
                {
                    var cachedPrices = await _cache.GetBulkPriceCacheAsync(symbolsList, vsCurrency);
                    if (cachedPrices.Any())
                    {
                        _logger.LogWarning("Returning cached data (may be expired) for multiple coins as fallback");
                        foreach (var kvp in cachedPrices)
                        {
                            kvp.Value.IsFromCache = true;
                        }
                        return cachedPrices;
                    }
                }
                
                throw;
            }
        }

        /// <summary>
        /// 获取币种历史价格数据
        /// Get historical price data for a coin
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <param name="vsCurrency">计价货币 / Vs currency</param>
        /// <param name="days">天数 / Number of days</param>
        /// <param name="forceRefresh">是否强制刷新缓存 / Whether to force refresh cache</param>
        /// <returns>历史价格数据点列表 / List of historical price data points</returns>
        public async Task<List<PriceDataPoint>> GetHistoricalDataAsync(string symbol, string vsCurrency = "usd", int days = 30, bool forceRefresh = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(symbol))
                {
                    throw new ArgumentException("Symbol cannot be empty", nameof(symbol));
                }

                string interval = GetIntervalForDays(days);
                
                // 检查缓存
                // Check cache
                if (!forceRefresh)
                {
                    var cachedData = await _cache.GetHistoricalDataCacheAsync(symbol, vsCurrency, days, interval);
                    if (cachedData != null && cachedData.Any() && 
                        !await _cache.IsCacheExpiredAsync(GetHistoricalCacheKey(symbol, vsCurrency, days, interval)))
                    {
                        _logger.LogDebug("Retrieved historical data for {Symbol} from cache", symbol);
                        return cachedData;
                    }
                }

                // 从API获取
                // Get from API
                var historicalData = await _apiClient.GetHistoricalDataAsync(symbol, vsCurrency, days);
                if (historicalData != null && historicalData.Any())
                {
                    // 存入缓存，历史数据缓存更长时间（6小时）
                    // Save to cache, historical data is cached for longer (6 hours)
                    await _cache.SetHistoricalDataCacheAsync(symbol, vsCurrency, days, interval, historicalData, 6);
                    _logger.LogDebug("Retrieved historical data for {Symbol} from API and cached", symbol);
                    return historicalData;
                }

                _logger.LogWarning("Failed to retrieve historical data for {Symbol}", symbol);
                return new List<PriceDataPoint>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting historical data for {Symbol}: {Message}", symbol, ex.Message);
                
                // 尝试从缓存获取过期数据作为后备
                // Try to get expired data from cache as fallback
                if (!forceRefresh)
                {
                    string interval = GetIntervalForDays(days);
                    var cachedData = await _cache.GetHistoricalDataCacheAsync(symbol, vsCurrency, days, interval);
                    if (cachedData != null && cachedData.Any())
                    {
                        _logger.LogWarning("Returning expired cache data for historical {Symbol} as fallback", symbol);
                        return cachedData;
                    }
                }
                
                throw;
            }
        }

        /// <summary>
        /// 获取币种市场信息
        /// Get market information for a coin
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <param name="forceRefresh">是否强制刷新缓存 / Whether to force refresh cache</param>
        /// <returns>市场信息 / Market information</returns>
        public async Task<CryptoMarketInfo> GetMarketInfoAsync(string symbol, bool forceRefresh = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(symbol))
                {
                    throw new ArgumentException("Symbol cannot be empty", nameof(symbol));
                }

                // 检查缓存
                // Check cache
                if (!forceRefresh)
                {
                    var cachedInfo = await _cache.GetMarketInfoCacheAsync(symbol);
                    if (cachedInfo != null && !await _cache.IsCacheExpiredAsync(GetMarketInfoCacheKey(symbol)))
                    {
                        _logger.LogDebug("Retrieved market info for {Symbol} from cache", symbol);
                        cachedInfo.IsFromCache = true;
                        return cachedInfo;
                    }
                }

                // 从API获取
                // Get from API
                var marketInfo = await _apiClient.GetMarketInfoAsync(symbol);
                if (marketInfo != null)
                {
                    marketInfo.IsFromCache = false;
                    // 存入缓存，市场信息缓存更长时间（6小时）
                    // Save to cache, market info is cached for longer (6 hours)
                    await _cache.SetMarketInfoCacheAsync(marketInfo, 6);
                    _logger.LogDebug("Retrieved market info for {Symbol} from API and cached", symbol);
                    return marketInfo;
                }

                _logger.LogWarning("Failed to retrieve market info for {Symbol}", symbol);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting market info for {Symbol}: {Message}", symbol, ex.Message);
                
                // 尝试从缓存获取过期数据作为后备
                // Try to get expired data from cache as fallback
                if (!forceRefresh)
                {
                    var cachedInfo = await _cache.GetMarketInfoCacheAsync(symbol);
                    if (cachedInfo != null)
                    {
                        _logger.LogWarning("Returning expired cache data for market info {Symbol} as fallback", symbol);
                        cachedInfo.IsFromCache = true;
                        return cachedInfo;
                    }
                }
                
                throw;
            }
        }

        /// <summary>
        /// 搜索币种
        /// Search for coins
        /// </summary>
        /// <param name="query">搜索关键词 / Search query</param>
        /// <returns>搜索结果列表 / List of search results</returns>
        public async Task<List<CryptoSearchResult>> SearchCoinsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Search query cannot be empty", nameof(query));
            }

            try
            {
                // 搜索结果不缓存，总是从API获取最新数据
                // Search results are not cached, always get the latest data from API
                var results = await _apiClient.SearchCoinsAsync(query);
                _logger.LogDebug("Searched for coins with query '{Query}', found {Count} results", query, results?.Count ?? 0);
                return results ?? new List<CryptoSearchResult>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching coins with query '{Query}': {Message}", query, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 获取热门币种
        /// Get trending coins
        /// </summary>
        /// <param name="forceRefresh">是否强制刷新缓存 / Whether to force refresh cache</param>
        /// <returns>热门币种列表 / List of trending coins</returns>
        public async Task<List<CryptoListItem>> GetTrendingCoinsAsync(bool forceRefresh = false)
        {
            try
            {
                // 检查缓存
                // Check cache
                if (!forceRefresh)
                {
                    var cachedTrending = await _cache.GetTrendingCoinsCacheAsync();
                    if (cachedTrending != null && cachedTrending.Any() && 
                        !await _cache.IsCacheExpiredAsync("trending_coins"))
                    {
                        _logger.LogDebug("Retrieved trending coins from cache");
                        return cachedTrending;
                    }
                }

                // 从API获取
                // Get from API
                var trendingCoins = await _apiClient.GetTrendingCoinsAsync();
                if (trendingCoins != null && trendingCoins.Any())
                {
                    // 存入缓存，热门币种缓存6小时
                    // Save to cache, trending coins are cached for 6 hours
                    await _cache.SetTrendingCoinsCacheAsync(trendingCoins, 6);
                    _logger.LogDebug("Retrieved trending coins from API and cached");
                    return trendingCoins;
                }

                _logger.LogWarning("Failed to retrieve trending coins");
                return new List<CryptoListItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trending coins: {Message}", ex.Message);
                
                // 尝试从缓存获取过期数据作为后备
                // Try to get expired data from cache as fallback
                if (!forceRefresh)
                {
                    var cachedTrending = await _cache.GetTrendingCoinsCacheAsync();
                    if (cachedTrending != null && cachedTrending.Any())
                    {
                        _logger.LogWarning("Returning expired cache data for trending coins as fallback");
                        return cachedTrending;
                    }
                }
                
                throw;
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
                await _cache.ClearAllCacheAsync();
                _logger.LogInformation("Cleared all market data cache");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing market data cache: {Message}", ex.Message);
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

            lock (_cacheExpirationLock)
            {
                _cacheExpirationMinutes = expirationTime;
                _logger.LogInformation("Cache expiration time set to {Minutes} minutes", expirationTime);
            }
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
        /// 获取价格缓存键
        /// Get price cache key
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <param name="currency">计价货币 / Currency</param>
        /// <returns>缓存键 / Cache key</returns>
        private string GetCacheKey(string symbol, string currency)
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
    }
} 