using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MultiChainWallet.Core.Models;

namespace MultiChainWallet.Infrastructure.Services
{
    /// <summary>
    /// CoinGecko API客户端
    /// CoinGecko API Client
    /// </summary>
    public class CoinGeckoApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CoinGeckoApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Dictionary<string, string> _coinIdCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private const string BASE_URL = "https://api.coingecko.com/api/v3";
        private DateTime _lastRequestTime = DateTime.MinValue;
        private const int REQUEST_THROTTLE_MS = 1200; // 限制请求频率，每秒不超过1次 / Limit request rate to no more than once per second

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="httpClient">HTTP客户端</param>
        /// <param name="logger">日志器</param>
        public CoinGeckoApiClient(HttpClient httpClient, ILogger<CoinGeckoApiClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // 设置基础URL
            // Set base URL
            _httpClient.BaseAddress = new Uri(BASE_URL);
            
            // 配置JSON序列化选项
            // Configure JSON serialization options
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            
            _logger.LogInformation("CoinGecko API客户端已初始化 / CoinGecko API client initialized");
        }

        /// <summary>
        /// 获取币种价格
        /// Get coin price
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <param name="vsCurrency">计价币种 / Vs currency</param>
        /// <returns>价格信息 / Price information</returns>
        public async Task<CryptoPriceInfo> GetCoinPriceAsync(string symbol, string vsCurrency = "usd")
        {
            if (string.IsNullOrEmpty(symbol))
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            string coinId = await GetCoinIdFromSymbolAsync(symbol);
            if (string.IsNullOrEmpty(coinId))
            {
                _logger.LogWarning("无法找到币种 {Symbol} 的ID / Cannot find ID for coin {Symbol}", symbol);
                return null;
            }

            await ThrottleRequestAsync();

            try
            {
                string endpoint = $"/coins/{coinId}?localization=false&tickers=false&market_data=true&community_data=false&developer_data=false&sparkline=false";
                var response = await _httpClient.GetFromJsonAsync<CoinDetailResponse>(endpoint, _jsonOptions);
                
                if (response?.market_data == null)
                {
                    _logger.LogWarning("获取币种 {Symbol} 的价格数据失败 / Failed to get price data for coin {Symbol}", symbol);
                    return null;
                }

                // 将响应数据转换为模型
                // Convert response data to model
                string normalizedCurrency = vsCurrency.ToLower();
                var priceInfo = new CryptoPriceInfo
                {
                    Symbol = symbol.ToUpper(),
                    Currency = vsCurrency.ToUpper(),
                    CurrentPrice = GetDecimalValue(response.market_data.current_price, normalizedCurrency),
                    PriceChange24h = GetDecimalValue(response.market_data.price_change_24h_in_currency, normalizedCurrency),
                    PriceChangePercentage24h = GetDecimalValue(response.market_data.price_change_percentage_24h_in_currency, normalizedCurrency),
                    High24h = GetDecimalValue(response.market_data.high_24h, normalizedCurrency),
                    Low24h = GetDecimalValue(response.market_data.low_24h, normalizedCurrency),
                    LastUpdated = DateTime.UtcNow
                };

                _logger.LogDebug("成功获取币种 {Symbol} 的价格数据 / Successfully got price data for coin {Symbol}", symbol);
                return priceInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取币种 {Symbol} 的价格数据时出错 / Error getting price data for coin {Symbol}", symbol);
                return null;
            }
        }

        /// <summary>
        /// 获取多个币种的价格
        /// Get prices for multiple coins
        /// </summary>
        /// <param name="symbols">币种符号列表 / List of coin symbols</param>
        /// <param name="vsCurrency">计价币种 / Vs currency</param>
        /// <returns>币种到价格信息的映射 / Mapping of coin symbols to price information</returns>
        public async Task<Dictionary<string, CryptoPriceInfo>> GetCoinsPricesAsync(IEnumerable<string> symbols, string vsCurrency = "usd")
        {
            if (symbols == null || !symbols.Any())
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            var result = new Dictionary<string, CryptoPriceInfo>();
            var symbolList = symbols.ToList();
            
            // 为了减少API调用，我们使用简单的价格API
            // To reduce API calls, we use the simple price API
            var coinIds = new List<string>();
            foreach (var symbol in symbolList)
            {
                string coinId = await GetCoinIdFromSymbolAsync(symbol);
                if (!string.IsNullOrEmpty(coinId))
                {
                    coinIds.Add(coinId);
                }
            }

            if (!coinIds.Any())
            {
                _logger.LogWarning("无法找到任何币种的ID / Cannot find ID for any coin");
                return result;
            }

            await ThrottleRequestAsync();

            try
            {
                string idsParam = string.Join(",", coinIds);
                string endpoint = $"/simple/price?ids={idsParam}&vs_currencies={vsCurrency}&include_market_cap=true&include_24hr_vol=true&include_24hr_change=true&include_last_updated_at=true";
                var response = await _httpClient.GetFromJsonAsync<Dictionary<string, Dictionary<string, JsonElement>>>(endpoint, _jsonOptions);
                
                if (response == null)
                {
                    _logger.LogWarning("获取多个币种的价格数据失败 / Failed to get price data for multiple coins");
                    return result;
                }

                // 将响应数据转换为模型
                // Convert response data to model
                foreach (var coinId in coinIds)
                {
                    if (response.TryGetValue(coinId, out var coinData) && coinData.TryGetValue(vsCurrency, out var priceElement))
                    {
                        string symbol = symbolList.FirstOrDefault(s => string.Equals(_coinIdCache[s], coinId, StringComparison.OrdinalIgnoreCase));
                        if (string.IsNullOrEmpty(symbol))
                        {
                            continue;
                        }

                        coinData.TryGetValue($"{vsCurrency}_24h_change", out var changeElement);
                        coinData.TryGetValue($"{vsCurrency}_market_cap", out var marketCapElement);
                        coinData.TryGetValue($"{vsCurrency}_24h_vol", out var volumeElement);
                        coinData.TryGetValue("last_updated_at", out var lastUpdatedElement);

                        var priceInfo = new CryptoPriceInfo
                        {
                            Symbol = symbol.ToUpper(),
                            Currency = vsCurrency.ToUpper(),
                            CurrentPrice = priceElement.TryGetDecimal(out var price) ? price : 0,
                            PriceChangePercentage24h = changeElement.TryGetDecimal(out var change) ? change : 0,
                            LastUpdated = lastUpdatedElement.TryGetInt64(out var timestamp) 
                                ? DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime 
                                : DateTime.UtcNow
                        };

                        result[symbol.ToUpper()] = priceInfo;
                    }
                }

                _logger.LogDebug("成功获取 {Count} 个币种的价格数据 / Successfully got price data for {Count} coins", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取多个币种的价格数据时出错 / Error getting price data for multiple coins");
                return result;
            }
        }

        /// <summary>
        /// 获取币种历史价格数据
        /// Get historical price data for a coin
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <param name="vsCurrency">计价币种 / Vs currency</param>
        /// <param name="days">天数 / Number of days</param>
        /// <returns>历史价格数据点列表 / List of historical price data points</returns>
        public async Task<List<PriceDataPoint>> GetHistoricalDataAsync(string symbol, string vsCurrency = "usd", int days = 30)
        {
            if (string.IsNullOrEmpty(symbol))
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            string coinId = await GetCoinIdFromSymbolAsync(symbol);
            if (string.IsNullOrEmpty(coinId))
            {
                _logger.LogWarning("无法找到币种 {Symbol} 的ID / Cannot find ID for coin {Symbol}", symbol);
                return null;
            }

            await ThrottleRequestAsync();

            try
            {
                string endpoint = $"/coins/{coinId}/market_chart?vs_currency={vsCurrency}&days={days}";
                var response = await _httpClient.GetFromJsonAsync<MarketChartResponse>(endpoint, _jsonOptions);
                
                if (response?.prices == null || !response.prices.Any())
                {
                    _logger.LogWarning("获取币种 {Symbol} 的历史价格数据失败 / Failed to get historical price data for coin {Symbol}", symbol);
                    return null;
                }

                // 将响应数据转换为模型
                // Convert response data to model
                var dataPoints = new List<PriceDataPoint>();
                for (int i = 0; i < response.prices.Count; i++)
                {
                    var pricePoint = response.prices[i];
                    var marketCapPoint = i < response.market_caps.Count ? response.market_caps[i] : new List<decimal> { 0, 0 };
                    var volumePoint = i < response.total_volumes.Count ? response.total_volumes[i] : new List<decimal> { 0, 0 };
                    
                    // CoinGecko返回的时间戳是毫秒级的
                    // CoinGecko returns timestamp in milliseconds
                    var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)pricePoint[0]).UtcDateTime;
                    
                    dataPoints.Add(new PriceDataPoint
                    {
                        Timestamp = timestamp,
                        Price = pricePoint[1],
                        MarketCap = marketCapPoint[1],
                        Volume = volumePoint[1]
                    });
                }

                _logger.LogDebug("成功获取币种 {Symbol} 的 {Count} 个历史价格数据点 / Successfully got {Count} historical price data points for coin {Symbol}", 
                    symbol, dataPoints.Count);
                return dataPoints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取币种 {Symbol} 的历史价格数据时出错 / Error getting historical price data for coin {Symbol}", symbol);
                return null;
            }
        }

        /// <summary>
        /// 获取币种市场信息
        /// Get market information for a coin
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <returns>市场信息 / Market information</returns>
        public async Task<CryptoMarketInfo> GetMarketInfoAsync(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            string coinId = await GetCoinIdFromSymbolAsync(symbol);
            if (string.IsNullOrEmpty(coinId))
            {
                _logger.LogWarning("无法找到币种 {Symbol} 的ID / Cannot find ID for coin {Symbol}", symbol);
                return null;
            }

            await ThrottleRequestAsync();

            try
            {
                string endpoint = $"/coins/{coinId}?localization=false&tickers=false&market_data=true&community_data=false&developer_data=false&sparkline=false";
                var response = await _httpClient.GetFromJsonAsync<CoinDetailResponse>(endpoint, _jsonOptions);
                
                if (response?.market_data == null)
                {
                    _logger.LogWarning("获取币种 {Symbol} 的市场信息失败 / Failed to get market information for coin {Symbol}", symbol);
                    return null;
                }

                // 将响应数据转换为模型
                // Convert response data to model
                var marketData = response.market_data;
                var marketInfo = new CryptoMarketInfo
                {
                    Symbol = symbol.ToUpper(),
                    Name = response.name,
                    IconUrl = response.image?.large,
                    MarketCap = GetDecimalValue(marketData.market_cap, "usd"),
                    MarketCapRank = marketData.market_cap_rank ?? 0,
                    FullyDilutedValuation = GetDecimalValue(marketData.fully_diluted_valuation, "usd"),
                    TotalSupply = marketData.total_supply ?? 0,
                    MaxSupply = marketData.max_supply,
                    CirculatingSupply = marketData.circulating_supply ?? 0,
                    Volume24h = GetDecimalValue(marketData.total_volume, "usd"),
                    LastUpdated = DateTime.UtcNow,
                    PriceInfo = new CryptoPriceInfo
                    {
                        Symbol = symbol.ToUpper(),
                        Currency = "USD",
                        CurrentPrice = GetDecimalValue(marketData.current_price, "usd"),
                        PriceChange24h = GetDecimalValue(marketData.price_change_24h_in_currency, "usd"),
                        PriceChangePercentage24h = GetDecimalValue(marketData.price_change_percentage_24h_in_currency, "usd"),
                        High24h = GetDecimalValue(marketData.high_24h, "usd"),
                        Low24h = GetDecimalValue(marketData.low_24h, "usd"),
                        LastUpdated = DateTime.UtcNow
                    }
                };

                _logger.LogDebug("成功获取币种 {Symbol} 的市场信息 / Successfully got market information for coin {Symbol}", symbol);
                return marketInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取币种 {Symbol} 的市场信息时出错 / Error getting market information for coin {Symbol}", symbol);
                return null;
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
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException(nameof(query));
            }

            await ThrottleRequestAsync();

            try
            {
                string endpoint = $"/search?query={Uri.EscapeDataString(query)}";
                var response = await _httpClient.GetFromJsonAsync<SearchResponse>(endpoint, _jsonOptions);
                
                if (response?.coins == null || !response.coins.Any())
                {
                    _logger.LogWarning("搜索币种 {Query} 未返回结果 / Search for coin {Query} returned no results", query);
                    return new List<CryptoSearchResult>();
                }

                // 将响应数据转换为模型
                // Convert response data to model
                var results = response.coins.Select(c => new CryptoSearchResult
                {
                    Id = c.id,
                    Symbol = c.symbol?.ToUpper(),
                    Name = c.name,
                    IconUrl = c.large,
                    MarketCapRank = c.market_cap_rank
                }).ToList();

                // 更新缓存
                // Update cache
                foreach (var result in results)
                {
                    if (!string.IsNullOrEmpty(result.Symbol) && !string.IsNullOrEmpty(result.Id))
                    {
                        _coinIdCache[result.Symbol] = result.Id;
                    }
                }

                _logger.LogDebug("搜索币种 {Query} 返回 {Count} 个结果 / Search for coin {Query} returned {Count} results", query, results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索币种 {Query} 时出错 / Error searching for coin {Query}", query);
                return new List<CryptoSearchResult>();
            }
        }

        /// <summary>
        /// 获取热门币种
        /// Get trending coins
        /// </summary>
        /// <returns>热门币种列表 / List of trending coins</returns>
        public async Task<List<CryptoListItem>> GetTrendingCoinsAsync()
        {
            await ThrottleRequestAsync();

            try
            {
                string endpoint = "/search/trending";
                var response = await _httpClient.GetFromJsonAsync<TrendingResponse>(endpoint, _jsonOptions);
                
                if (response?.coins == null || !response.coins.Any())
                {
                    _logger.LogWarning("获取热门币种失败 / Failed to get trending coins");
                    return new List<CryptoListItem>();
                }

                // 将响应数据转换为模型
                // Convert response data to model
                var trendingCoins = response.coins.Select(c => new CryptoListItem
                {
                    Id = c.item.id,
                    Symbol = c.item.symbol?.ToUpper(),
                    Name = c.item.name,
                    IconUrl = c.item.large,
                    CurrentPrice = c.item.price_btc,
                    MarketCapRank = c.item.market_cap_rank ?? 0,
                    PriceChangePercentage24h = 0 // 趋势API中没有这个数据 / This data is not available in the trending API
                }).ToList();

                // 更新缓存
                // Update cache
                foreach (var coin in trendingCoins)
                {
                    if (!string.IsNullOrEmpty(coin.Symbol) && !string.IsNullOrEmpty(coin.Id))
                    {
                        _coinIdCache[coin.Symbol] = coin.Id;
                    }
                }

                _logger.LogDebug("成功获取 {Count} 个热门币种 / Successfully got {Count} trending coins", trendingCoins.Count);
                return trendingCoins;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取热门币种时出错 / Error getting trending coins");
                return new List<CryptoListItem>();
            }
        }

        /// <summary>
        /// 根据符号获取币种ID
        /// Get coin ID from symbol
        /// </summary>
        /// <param name="symbol">币种符号 / Coin symbol</param>
        /// <returns>币种ID / Coin ID</returns>
        private async Task<string> GetCoinIdFromSymbolAsync(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
            {
                return null;
            }

            // 检查缓存
            // Check cache
            if (_coinIdCache.TryGetValue(symbol, out string coinId))
            {
                return coinId;
            }

            // 在API中搜索
            // Search in API
            var searchResults = await SearchCoinsAsync(symbol);
            var exactMatch = searchResults.FirstOrDefault(r => string.Equals(r.Symbol, symbol, StringComparison.OrdinalIgnoreCase));
            
            if (exactMatch != null)
            {
                _coinIdCache[symbol] = exactMatch.Id;
                return exactMatch.Id;
            }

            // 如果没有找到完全匹配的结果，尝试使用第一个结果
            // If no exact match found, try using the first result
            var firstResult = searchResults.FirstOrDefault();
            if (firstResult != null)
            {
                _coinIdCache[symbol] = firstResult.Id;
                return firstResult.Id;
            }

            return null;
        }

        /// <summary>
        /// 限制请求频率
        /// Throttle request rate
        /// </summary>
        private async Task ThrottleRequestAsync()
        {
            var elapsed = DateTime.UtcNow - _lastRequestTime;
            var remainingMs = REQUEST_THROTTLE_MS - (int)elapsed.TotalMilliseconds;
            
            if (remainingMs > 0)
            {
                await Task.Delay(remainingMs);
            }
            
            _lastRequestTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 从字典中获取decimal值
        /// Get decimal value from dictionary
        /// </summary>
        private decimal GetDecimalValue(Dictionary<string, decimal> dict, string key)
        {
            if (dict != null && dict.TryGetValue(key, out decimal value))
            {
                return value;
            }
            return 0;
        }

        // 响应模型类
        // Response model classes

        private class CoinDetailResponse
        {
            public string id { get; set; }
            public string symbol { get; set; }
            public string name { get; set; }
            public ImageData image { get; set; }
            public MarketData market_data { get; set; }

            public class ImageData
            {
                public string thumb { get; set; }
                public string small { get; set; }
                public string large { get; set; }
            }

            public class MarketData
            {
                public Dictionary<string, decimal> current_price { get; set; }
                public Dictionary<string, decimal> high_24h { get; set; }
                public Dictionary<string, decimal> low_24h { get; set; }
                public Dictionary<string, decimal> price_change_24h_in_currency { get; set; }
                public Dictionary<string, decimal> price_change_percentage_24h_in_currency { get; set; }
                public Dictionary<string, decimal> market_cap { get; set; }
                public Dictionary<string, decimal> total_volume { get; set; }
                public Dictionary<string, decimal> fully_diluted_valuation { get; set; }
                public int? market_cap_rank { get; set; }
                public decimal? total_supply { get; set; }
                public decimal? max_supply { get; set; }
                public decimal? circulating_supply { get; set; }
            }
        }

        private class MarketChartResponse
        {
            public List<List<decimal>> prices { get; set; }
            public List<List<decimal>> market_caps { get; set; }
            public List<List<decimal>> total_volumes { get; set; }
        }

        private class SearchResponse
        {
            public List<CoinSearchResult> coins { get; set; }

            public class CoinSearchResult
            {
                public string id { get; set; }
                public string name { get; set; }
                public string symbol { get; set; }
                public string large { get; set; }
                public int? market_cap_rank { get; set; }
            }
        }

        private class TrendingResponse
        {
            public List<TrendingCoin> coins { get; set; }

            public class TrendingCoin
            {
                public TrendingCoinItem item { get; set; }
            }

            public class TrendingCoinItem
            {
                public string id { get; set; }
                public string name { get; set; }
                public string symbol { get; set; }
                public string large { get; set; }
                public int? market_cap_rank { get; set; }
                public decimal price_btc { get; set; }
            }
        }
    }

    // JsonElement扩展，用于帮助处理JSON响应
    // JsonElement extension to help process JSON responses
    internal static class JsonElementExtensions
    {
        public static bool TryGetDecimal(this JsonElement element, out decimal value)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Number:
                    return element.TryGetDecimal(out value);
                case JsonValueKind.String:
                    return decimal.TryParse(element.GetString(), out value);
                default:
                    value = 0;
                    return false;
            }
        }
        
        public static bool TryGetInt64(this JsonElement element, out long value)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Number:
                    return element.TryGetInt64(out value);
                case JsonValueKind.String:
                    return long.TryParse(element.GetString(), out value);
                default:
                    value = 0;
                    return false;
            }
        }
    }
}