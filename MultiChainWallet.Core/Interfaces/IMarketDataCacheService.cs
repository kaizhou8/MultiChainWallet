using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MultiChainWallet.Core.Models;

namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// 市场数据缓存服务接口，负责本地存储市场数据
    /// Market data cache service interface, responsible for local storage of market data
    /// </summary>
    public interface IMarketDataCacheService
    {
        /// <summary>
        /// 保存币种价格
        /// Save coin price
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <param name="vsCurrency">计价货币 / Vs currency</param>
        /// <param name="priceInfo">价格信息 / Price information</param>
        Task SaveCoinPriceAsync(string symbol, string vsCurrency, CryptoPriceInfo priceInfo);

        /// <summary>
        /// 获取缓存的币种价格
        /// Get cached coin price
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <param name="vsCurrency">计价货币 / Vs currency</param>
        /// <returns>价格信息，如果没有缓存则返回null / Price information, or null if not cached</returns>
        Task<CryptoPriceInfo> GetCachedCoinPriceAsync(string symbol, string vsCurrency);

        /// <summary>
        /// 缓存是否有效
        /// Whether the cache is valid
        /// </summary>
        /// <param name="cacheDate">缓存日期 / Cache date</param>
        /// <returns>缓存是否有效 / Whether cache is valid</returns>
        bool IsCacheValid(DateTime cacheDate);

        /// <summary>
        /// 保存历史价格数据
        /// Save historical price data
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <param name="vsCurrency">计价货币 / Vs currency</param>
        /// <param name="days">天数 / Number of days</param>
        /// <param name="dataPoints">历史价格数据点 / Historical price data points</param>
        Task SaveHistoricalDataAsync(string symbol, string vsCurrency, int days, List<PriceDataPoint> dataPoints);

        /// <summary>
        /// 获取缓存的历史价格数据
        /// Get cached historical price data
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <param name="vsCurrency">计价货币 / Vs currency</param>
        /// <param name="days">天数 / Number of days</param>
        /// <returns>历史价格数据点，如果没有缓存则返回null / Historical price data points, or null if not cached</returns>
        Task<List<PriceDataPoint>> GetCachedHistoricalDataAsync(string symbol, string vsCurrency, int days);

        /// <summary>
        /// 保存市场信息
        /// Save market information
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <param name="marketInfo">市场信息 / Market information</param>
        Task SaveMarketInfoAsync(string symbol, CryptoMarketInfo marketInfo);

        /// <summary>
        /// 获取缓存的市场信息
        /// Get cached market information
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <returns>市场信息，如果没有缓存则返回null / Market information, or null if not cached</returns>
        Task<CryptoMarketInfo> GetCachedMarketInfoAsync(string symbol);

        /// <summary>
        /// 保存热门币种
        /// Save trending coins
        /// </summary>
        /// <param name="trendingCoins">热门币种列表 / List of trending coins</param>
        Task SaveTrendingCoinsAsync(List<CryptoListItem> trendingCoins);

        /// <summary>
        /// 获取缓存的热门币种
        /// Get cached trending coins
        /// </summary>
        /// <returns>热门币种列表，如果没有缓存则返回null / List of trending coins, or null if not cached</returns>
        Task<List<CryptoListItem>> GetCachedTrendingCoinsAsync();

        /// <summary>
        /// 清除缓存
        /// Clear cache
        /// </summary>
        Task ClearCacheAsync();

        /// <summary>
        /// 设置缓存过期时间
        /// Set cache expiration time
        /// </summary>
        /// <param name="expirationTime">过期时间（分钟） / Expiration time (minutes)</param>
        void SetCacheExpirationTime(int expirationTime);
    }
} 