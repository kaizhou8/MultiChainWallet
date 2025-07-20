using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MultiChainWallet.Core.Models;

namespace MultiChainWallet.Core.Interfaces
{
    /// <summary>
    /// 市场数据服务接口
    /// Market data service interface
    /// </summary>
    public interface IMarketDataService
    {
        /// <summary>
        /// 获取币种价格
        /// Get coin price
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <param name="vsCurrency">计价货币 / Vs currency</param>
        /// <param name="forceRefresh">是否强制刷新缓存 / Whether to force refresh cache</param>
        /// <returns>价格信息 / Price information</returns>
        Task<CryptoPriceInfo> GetCoinPriceAsync(string symbol, string vsCurrency = "usd", bool forceRefresh = false);

        /// <summary>
        /// 获取多个币种的价格
        /// Get prices for multiple coins
        /// </summary>
        /// <param name="symbols">币种符号列表 / List of coin symbols</param>
        /// <param name="vsCurrency">计价货币 / Vs currency</param>
        /// <param name="forceRefresh">是否强制刷新缓存 / Whether to force refresh cache</param>
        /// <returns>币种到价格信息的映射 / Mapping of coin symbols to price information</returns>
        Task<Dictionary<string, CryptoPriceInfo>> GetCoinsPricesAsync(IEnumerable<string> symbols, string vsCurrency = "usd", bool forceRefresh = false);

        /// <summary>
        /// 获取币种历史价格数据
        /// Get historical price data for a coin
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <param name="vsCurrency">计价货币 / Vs currency</param>
        /// <param name="days">天数 / Number of days</param>
        /// <param name="forceRefresh">是否强制刷新缓存 / Whether to force refresh cache</param>
        /// <returns>历史价格数据点列表 / List of historical price data points</returns>
        Task<List<PriceDataPoint>> GetHistoricalDataAsync(string symbol, string vsCurrency = "usd", int days = 30, bool forceRefresh = false);

        /// <summary>
        /// 获取币种市场信息
        /// Get market information for a coin
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <param name="forceRefresh">是否强制刷新缓存 / Whether to force refresh cache</param>
        /// <returns>市场信息 / Market information</returns>
        Task<CryptoMarketInfo> GetMarketInfoAsync(string symbol, bool forceRefresh = false);

        /// <summary>
        /// 搜索币种
        /// Search for coins
        /// </summary>
        /// <param name="query">搜索关键词 / Search query</param>
        /// <returns>搜索结果列表 / List of search results</returns>
        Task<List<CryptoSearchResult>> SearchCoinsAsync(string query);

        /// <summary>
        /// 获取热门币种
        /// Get trending coins
        /// </summary>
        /// <param name="forceRefresh">是否强制刷新缓存 / Whether to force refresh cache</param>
        /// <returns>热门币种列表 / List of trending coins</returns>
        Task<List<CryptoListItem>> GetTrendingCoinsAsync(bool forceRefresh = false);

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