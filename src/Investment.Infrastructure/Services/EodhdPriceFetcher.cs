using Investment.Domain.Entities;
using Investment.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace Investment.Infrastructure.Services;

public class EodhdPriceFetcher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EodhdPriceFetcher> _logger;
    private readonly IConfiguration _configuration;
    private readonly InvestmentDbContext _context;

    public EodhdPriceFetcher(
        HttpClient httpClient,
        ILogger<EodhdPriceFetcher> logger,
        IConfiguration configuration,
        InvestmentDbContext context)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _context = context;
    }

    private async Task<IReadOnlyList<string>> LoadApiKeysAsync()
    {
        var keys = new List<string>();
        var dbKeys = await _context.AppSettings
            .Where(s => s.SettingKey == "EodhdApiKey" || s.SettingKey == "EodhdApiKey2")
            .OrderBy(s => s.SettingKey == "EodhdApiKey" ? 0 : 1)
            .Select(s => s.SettingValue)
            .ToListAsync();

        AddKeys(keys, dbKeys);
        AddKeys(keys, new[]
        {
            _configuration["PriceProviders:EodhdApiKey"],
            _configuration["PriceProviders:EodhdApiKey2"]
        });

        return keys;
    }

    private static void AddKeys(List<string> keys, IEnumerable<string?> candidates)
    {
        foreach (var candidate in candidates)
        {
            var key = candidate?.Trim();
            if (!string.IsNullOrWhiteSpace(key)
                && !key.Equals("demo", StringComparison.OrdinalIgnoreCase)
                && !keys.Contains(key))
            {
                keys.Add(key);
            }
        }
    }

    public async Task<List<(int AssetId, decimal Price, DateTime Date)>> FetchPricesAsync(IEnumerable<Asset> assets)
    {
        var results = new List<(int AssetId, decimal Price, DateTime Date)>();
        var apiKeys = await LoadApiKeysAsync();
        if (apiKeys.Count == 0)
            return results;

        var keyRemaining = await LoadRemainingByKeyAsync(apiKeys);

        foreach (var asset in assets)
        {
            if (string.IsNullOrWhiteSpace(asset.ExternalTicker))
                continue;

            var keyIndex = SelectKeyWithCapacity(keyRemaining);
            if (keyIndex < 0)
            {
                _logger.LogWarning("EODHD: All API keys exhausted; stopping price fetch after {Count} assets", results.Count);
                break;
            }

            try
            {
                var symbol = NormalizeSymbol(asset.ExternalTicker);
                var latestPrice = await FetchLatestEodPriceAsync(asset.AssetId, symbol, apiKeys[keyIndex]);
                if (latestPrice != null)
                {
                    results.Add(latestPrice.Value);
                    keyRemaining[keyIndex] = Math.Max(0, keyRemaining[keyIndex] - 1);
                }
                else
                {
                    var fallbackIndex = TryNextKeyWithCapacity(keyRemaining, keyIndex);
                    if (fallbackIndex >= 0)
                    {
                        latestPrice = await FetchLatestEodPriceAsync(asset.AssetId, symbol, apiKeys[fallbackIndex]);
                        if (latestPrice != null)
                        {
                            results.Add(latestPrice.Value);
                            keyRemaining[fallbackIndex] = Math.Max(0, keyRemaining[fallbackIndex] - 1);
                        }
                    }
                }

                await Task.Delay(200);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "EODHD: Error fetching price for {Ticker}", asset.ExternalTicker);
            }
        }

        return results;
    }

    public async Task<List<(string Code, string Name, string Type, string Currency, string ExternalTicker)>> SearchAssetsAsync(string query, int limit = 20)
    {
        var results = new List<(string Code, string Name, string Type, string Currency, string ExternalTicker)>();

        if (string.IsNullOrWhiteSpace(query))
            return results;

        var apiKeys = await LoadApiKeysAsync();
        foreach (var apiKey in apiKeys)
        {
            var found = await SearchAssetsWithKeyAsync(query, limit, apiKey);
            if (found.Count > 0)
                return found;
        }

        return results;
    }

    public async Task<(decimal Price, DateTime Date)?> FetchLatestPriceAsync(string externalTicker)
    {
        if (string.IsNullOrWhiteSpace(externalTicker))
            return null;

        var apiKeys = await LoadApiKeysAsync();
        if (apiKeys.Count == 0)
            return null;

        var keyRemaining = await LoadRemainingByKeyAsync(apiKeys);
        var symbol = NormalizeSymbol(externalTicker);

        while (true)
        {
            var keyIndex = SelectKeyWithCapacity(keyRemaining);
            if (keyIndex < 0)
                return null;

            var latestPrice = await FetchLatestEodPriceAsync(0, symbol, apiKeys[keyIndex]);
            keyRemaining[keyIndex] = Math.Max(0, keyRemaining[keyIndex] - 1);

            if (latestPrice != null)
                return (latestPrice.Value.Price, latestPrice.Value.Date);
        }
    }

    private async Task<List<(string Code, string Name, string Type, string Currency, string ExternalTicker)>> SearchAssetsWithKeyAsync(
        string query, int limit, string apiKey)
    {
        var results = new List<(string Code, string Name, string Type, string Currency, string ExternalTicker)>();

        try
        {
            var normalizedQuery = query.Trim();
            var url = $"https://eodhd.com/api/exchange-symbol-list/EGX?api_token={apiKey}&fmt=json";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("EODHD: Failed to search assets for {Query}. Status: {Status}",
                    query, response.StatusCode);
                return results;
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<List<EodhdSearchResponse>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (data == null)
                return results;

            foreach (var item in data
                         .Where(item => MatchesQuery(item, normalizedQuery))
                         .Take(limit))
            {
                if (string.IsNullOrWhiteSpace(item.Code))
                    continue;

                var exchange = string.IsNullOrWhiteSpace(item.Exchange) ? "EGX" : item.Exchange.Trim().ToUpperInvariant();
                var code = item.Code.Trim().ToUpperInvariant();
                var externalTicker = code.Contains('.') ? code : $"{code}.{exchange}";

                results.Add((
                    code.Contains('.') ? code.Split('.')[0] : code,
                    string.IsNullOrWhiteSpace(item.Name) ? code : item.Name.Trim(),
                    "Stock",
                    string.IsNullOrWhiteSpace(item.Currency) ? "EGP" : item.Currency.Trim(),
                    externalTicker));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EODHD: Error searching assets for {Query}", query);
        }

        return results;
    }

    private async Task<(int AssetId, decimal Price, DateTime Date)?> FetchLatestEodPriceAsync(int assetId, string symbol, string apiKey)
    {
        try
        {
            var url = $"https://eodhd.com/api/eod/{WebUtility.UrlEncode(symbol)}?api_token={apiKey}&fmt=json";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("EODHD: Failed to fetch EOD price for {Ticker}. Status: {Status}",
                    symbol, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<List<EodhdEodResponse>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (data == null || data.Count == 0)
                return null;

            var latest = data
                .Select(item => new
                {
                    Item = item,
                    HasDate = DateTime.TryParse(item.Date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsedDate),
                    Date = parsedDate
                })
                .Where(x => x.HasDate)
                .OrderBy(x => x.Date)
                .LastOrDefault();

            if (latest == null)
                return null;

            var latestItem = latest.Item;
            var date = latest.Date;

            if (latestItem.Close <= 0)
            {
                _logger.LogWarning("EODHD: Latest EOD price for {Ticker} has invalid close value {Price} on {Date}",
                    symbol, latestItem.Close, latestItem.Date);
                return null;
            }

            _logger.LogInformation("EODHD: Fetched EOD price for {Ticker}: {Price} on {Date}",
                symbol, latestItem.Close, latestItem.Date);

            return (assetId, latestItem.Close, date);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EODHD: Error fetching EOD fallback for {Ticker}", symbol);
            return null;
        }
    }

    private static string NormalizeSymbol(string ticker)
    {
        var trimmed = ticker.Trim().ToUpperInvariant();
        return trimmed.Contains('.') ? trimmed : $"{trimmed}.EGX";
    }

    private static bool MatchesQuery(EodhdSearchResponse item, string query)
    {
        return item.Code.Contains(query, StringComparison.OrdinalIgnoreCase)
            || item.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private class EodhdEodResponse
    {
        public string Date { get; set; } = string.Empty;
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Adjusted_close { get; set; }
        public long Volume { get; set; }
    }

    private class EodhdSearchResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
    }

    public class EodhdUserInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SubscriptionType { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public int ApiRequests { get; set; }
        public string ApiRequestsDate { get; set; } = string.Empty;
        public int DailyRateLimit { get; set; }
        public int ExtraLimit { get; set; }
        public string? InviteToken { get; set; }
        public int InviteTokenClicked { get; set; }
        public string SubscriptionMode { get; set; } = string.Empty;
        public bool CanManageOrganizations { get; set; }
    }

    public class EodhdKeyStatus
    {
        public int Index { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SubscriptionType { get; set; } = string.Empty;
        public int ApiRequestsUsedToday { get; set; }
        public int DailyRateLimit { get; set; }
        public int ExtraLimit { get; set; }
        public int TotalAvailable { get; set; }
        public int Remaining { get; set; }
        public bool Available { get; set; }
    }

    public class EodhdAggregatedStatus
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SubscriptionType { get; set; } = string.Empty;
        public int ApiRequestsUsedToday { get; set; }
        public int DailyRateLimit { get; set; }
        public int ExtraLimit { get; set; }
        public int TotalAvailable { get; set; }
        public int Remaining { get; set; }
        public int KeyCount { get; set; }
        public List<EodhdKeyStatus> Keys { get; set; } = new();
    }

    public static int CalculateRemaining(EodhdUserInfo info)
    {
        var remaining = info.DailyRateLimit - GetApiRequestsUsedToday(info) + info.ExtraLimit;
        return remaining < 0 ? 0 : remaining;
    }

    public static int GetApiRequestsUsedToday(EodhdUserInfo info)
    {
        if (!DateTime.TryParse(info.ApiRequestsDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var apiRequestsDate))
            return info.ApiRequests;

        return apiRequestsDate.Date == DateTime.UtcNow.Date ? info.ApiRequests : 0;
    }

    public async Task<EodhdUserInfo?> GetUserInfoAsync()
    {
        var aggregated = await GetAggregatedStatusAsync();
        if (aggregated == null || aggregated.Keys.Count == 0)
            return null;

        var first = aggregated.Keys.FirstOrDefault(k => k.Available);
        if (first == null)
            return null;

        return new EodhdUserInfo
        {
            Name = first.Name,
            Email = first.Email,
            SubscriptionType = first.SubscriptionType,
            ApiRequests = aggregated.ApiRequestsUsedToday,
            DailyRateLimit = aggregated.DailyRateLimit,
            ExtraLimit = aggregated.ExtraLimit
        };
    }

    public async Task<EodhdAggregatedStatus?> GetAggregatedStatusAsync()
    {
        var apiKeys = await LoadApiKeysAsync();
        if (apiKeys.Count == 0)
            return null;

        var keyStatuses = new List<EodhdKeyStatus>();

        for (var i = 0; i < apiKeys.Count; i++)
        {
            var info = await GetUserInfoForKeyAsync(apiKeys[i]);
            if (info == null)
            {
                keyStatuses.Add(new EodhdKeyStatus
                {
                    Index = i + 1,
                    Label = $"مفتاح {i + 1}",
                    Available = false
                });
                continue;
            }

            var remaining = CalculateRemaining(info);
            var apiRequestsUsedToday = GetApiRequestsUsedToday(info);
            keyStatuses.Add(new EodhdKeyStatus
            {
                Index = i + 1,
                Label = $"مفتاح {i + 1}",
                Name = info.Name,
                Email = info.Email,
                SubscriptionType = info.SubscriptionType,
                ApiRequestsUsedToday = apiRequestsUsedToday,
                DailyRateLimit = info.DailyRateLimit,
                ExtraLimit = info.ExtraLimit,
                TotalAvailable = info.DailyRateLimit + info.ExtraLimit,
                Remaining = remaining,
                Available = remaining > 0
            });
        }

        if (keyStatuses.Count == 0)
            return null;

        var availableKeys = keyStatuses.Where(k => k.Available).ToList();
        var primary = availableKeys.FirstOrDefault() ?? keyStatuses[0];

        return new EodhdAggregatedStatus
        {
            Name = primary.Name,
            Email = primary.Email,
            SubscriptionType = primary.SubscriptionType,
            ApiRequestsUsedToday = keyStatuses.Sum(k => k.ApiRequestsUsedToday),
            DailyRateLimit = keyStatuses.Sum(k => k.DailyRateLimit),
            ExtraLimit = keyStatuses.Sum(k => k.ExtraLimit),
            TotalAvailable = keyStatuses.Sum(k => k.TotalAvailable),
            Remaining = keyStatuses.Sum(k => k.Remaining),
            KeyCount = apiKeys.Count,
            Keys = keyStatuses
        };
    }

    public async Task<bool> HasConfiguredApiKeyAsync()
    {
        var apiKeys = await LoadApiKeysAsync();
        return apiKeys.Count > 0;
    }

    public async Task<EodhdUserInfo?> ValidateApiKeyAsync(string apiKey)
    {
        var key = apiKey.Trim();
        return string.IsNullOrWhiteSpace(key) ? null : await GetUserInfoForKeyAsync(key);
    }

    private async Task<EodhdUserInfo?> GetUserInfoForKeyAsync(string apiKey)
    {
        try
        {
            var url = $"https://eodhd.com/api/user?api_token={apiKey}&fmt=json";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("EODHD: Failed to fetch user info. Status: {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<EodhdUserInfo>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EODHD: Error fetching user info");
            return null;
        }
    }

    private async Task<Dictionary<int, int>> LoadRemainingByKeyAsync(IReadOnlyList<string> apiKeys)
    {
        var remaining = new Dictionary<int, int>();
        for (var i = 0; i < apiKeys.Count; i++)
        {
            var info = await GetUserInfoForKeyAsync(apiKeys[i]);
            remaining[i] = info == null ? 0 : CalculateRemaining(info);
        }

        return remaining;
    }

    private static int SelectKeyWithCapacity(Dictionary<int, int> keyRemaining)
    {
        foreach (var entry in keyRemaining.OrderBy(e => e.Key))
        {
            if (entry.Value > 0)
                return entry.Key;
        }

        return -1;
    }

    private static int TryNextKeyWithCapacity(Dictionary<int, int> keyRemaining, int currentKeyIndex)
    {
        foreach (var entry in keyRemaining.OrderBy(e => e.Key))
        {
            if (entry.Key != currentKeyIndex && entry.Value > 0)
                return entry.Key;
        }

        return -1;
    }
}
