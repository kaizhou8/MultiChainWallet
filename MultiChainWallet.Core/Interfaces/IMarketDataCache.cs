using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MultiChainWallet.Core.Models;

namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// 市场数据缓存接口，提供市场数据的本地缓存功能
    /// Market data cache interface that provides local caching for market data
    /// </summary>
    public interface IMarketDataCache
    {
        /// <summary>
        /// 获取价格缓存
        /// Get price cache
        /// </summary>
        /// <param name="symbol">币种符号 / Currency symbol</param>
        /// <param name="currency">法币符号 / Fiat currency symbol</param>
        /// <returns>价格信息，如果缓存不存在则返回null / Price info, or null if cache doesn't exist</returns>
        Task<CryptoPriceInfo> GetPriceCacheAsync(string symbol, string currency);

        /// <summary>
        /// 设置价格缓存
        /// Set price cache
        /// </summary>
        /// <param name="priceInfo">价格信息 / Price information</param>
        /// <param name="expirationMinutes">缓存过期时间（分钟） / Cache expiration time in minutes</param>
        Task SetPriceCacheAsync(CryptoPriceInfo priceInfo, int expirationMinutes = 15);

        /// <summary>
        /// 获取批量价格缓存
        /// Get bulk price cache
        /// </summary>
        /// <param name="symbols">币种符号列表 / Currency symbol list</param>
        /// <param name="currency">法币符号 / Fiat currency symbol</param>
        /// <returns>币种符号到价格信息的映射 / Mapping of currency symbols to price information</returns>
        Task<Dictionary<string, CryptoPriceInfo>> GetBulkPriceCacheAsync(IEnumerable<string> symbols, string currency);

        /// <summary>
        /// 设置批量价格缓存
        /// Set bulk price cache
        /// </summary>
        /// <param name="pricesInfo">币种符号到价格信息的映射 / Mapping of currency symbols to price information</param>
        /// <param name="expirationMinutes">缓存过期时间（分钟） / Cache expiration time in minutes</param>
        Task SetBulkPriceCacheAsync(Dictionary<string, CryptoPriceInfo> pricesInfo, int expirationMinutes = 15);

        /// <summary>
        /// 获取历史数据缓存
        /// Get historical data cache
        /// </summary>
        /// <param name="symbol">币种符号 / Currency symbol</param>
        /// <param name="currency">法币符号 / Fiat currency symbol</param>
        /// <param name="days">天数 / Number of days</param>
        /// <param name="interval">数据间隔 / Data interval</param>
        /// <returns>历史价格数据点列表 / List of historical price data points</returns>
        Task<List<PriceDataPoint>> GetHistoricalDataCacheAsync(string symbol, string currency, int days, string interval);

        /// <summary>
        /// 设置历史数据缓存
        /// Set historical data cache
        /// </summary>
        /// <param name="symbol">币种符号 / Currency symbol</param>
        /// <param name="currency">法币符号 / Fiat currency symbol</param>
        /// <param name="days">天数 / Number of days</param>
        /// <param name="interval">数据间隔 / Data interval</param>
        /// <param name="dataPoints">历史价格数据点列表 / List of historical price data points</param>
        /// <param name="expirationHours">缓存过期时间（小时） / Cache expiration time in hours</param>
        Task SetHistoricalDataCacheAsync(string symbol, string currency, int days, string interval, List<PriceDataPoint> dataPoints, int expirationHours = 6);

        /// <summary>
        /// 获取市场信息缓存
        /// Get market info cache
        /// </summary>
        /// <param name="symbol">币种符号 / Currency symbol</param>
        /// <returns>市场信息 / Market information</returns>
        Task<CryptoMarketInfo> GetMarketInfoCacheAsync(string symbol);

        /// <summary>
        /// 设置市场信息缓存
        /// Set market info cache
        /// </summary>
        /// <param name="marketInfo">市场信息 / Market information</param>
        /// <param name="expirationHours">缓存过期时间（小时） / Cache expiration time in hours</param>
        Task SetMarketInfoCacheAsync(CryptoMarketInfo marketInfo, int expirationHours = 6);

        /// <summary>
        /// 获取热门币种缓存
        /// Get trending coins cache
        /// </summary>
        /// <returns>热门币种列表 / List of trending coins</returns>
        Task<List<CryptoListItem>> GetTrendingCoinsCacheAsync();

        /// <summary>
        /// 设置热门币种缓存
        /// Set trending coins cache
        /// </summary>
        /// <param name="trendingCoins">热门币种列表 / List of trending coins</param>
        /// <param name="expirationHours">缓存过期时间（小时） / Cache expiration time in hours</param>
        Task SetTrendingCoinsCacheAsync(List<CryptoListItem> trendingCoins, int expirationHours = 6);

        /// <summary>
        /// 清除特定币种的所有缓存
        /// Clear all caches for a specific coin
        /// </summary>
        /// <param name="symbol">币种符号 / Currency symbol</param>
        Task ClearCoinCacheAsync(string symbol);

        /// <summary>
        /// 清除所有缓存
        /// Clear all caches
        /// </summary>
        Task ClearAllCacheAsync();

        /// <summary>
        /// 缓存是否过期
        /// Check if cache is expired
        /// </summary>
        /// <param name="cacheKey">缓存键 / Cache key</param>
        /// <returns>是否过期 / Whether expired</returns>
        Task<bool> IsCacheExpiredAsync(string cacheKey);
    }
} 