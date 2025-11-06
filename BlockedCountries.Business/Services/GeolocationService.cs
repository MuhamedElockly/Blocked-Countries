using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BlockedCountries.Business.Configuration;
using BlockedCountries.Business.Models;

namespace BlockedCountries.Business.Services;

public class GeolocationService : IGeolocationService
{
    private readonly HttpClient _httpClient;
    private readonly GeolocationApiConfig _config;
    private readonly ILogger<GeolocationService> _logger;
    private readonly SemaphoreSlim _rateLimiter;
    private DateTime _lastRequestTime = DateTime.MinValue;
    private readonly object _lockObject = new();

    public GeolocationService(
        HttpClient httpClient,
        IOptions<GeolocationApiConfig> config,
        ILogger<GeolocationService> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
        _rateLimiter = new SemaphoreSlim(_config.RateLimitPerMinute, _config.RateLimitPerMinute);

        if (_httpClient.Timeout == TimeSpan.FromSeconds(100))
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }
    }

    public async Task<IpLookupResponse?> LookupIpAddressAsync(string ipAddress)
    {
        if (!IsValidIpAddress(ipAddress))
        {
            _logger.LogWarning("Invalid IP address format: {IpAddress}", ipAddress);
            return null;
        }

        try
        {
            await _rateLimiter.WaitAsync();
            try
            {
                await EnforceRateLimitAsync();

                var url = $"/{ipAddress}/json/";

                var response = await _httpClient.GetAsync(url);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Rate limit exceeded for IP lookup: {IpAddress}", ipAddress);
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("HTTP error {StatusCode} for IP lookup {IpAddress}: {Error}",
                        response.StatusCode, ipAddress, errorBody);
                    return null;
                }


                return await ParseIpApiResponseAsync(ipAddress, response);

            }
            finally
            {
                _rateLimiter.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while looking up IP address: {IpAddress}", ipAddress);
            return null;
        }
    }

    private async Task<IpLookupResponse?> ParseIpApiResponseAsync(string ipAddress, HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;


        if (root.TryGetProperty("error", out var errorProp) &&
            (errorProp.ValueKind == JsonValueKind.True ||
             errorProp.GetString()?.ToLower() == "true"))
        {
            var reason = root.TryGetProperty("reason", out var reasonProp)
                ? reasonProp.GetString() ?? "Unknown error"
                : "Unknown error";

            _logger.LogWarning("API returned error for IP {IpAddress}: {Reason}", ipAddress, reason);
            return null;
        }


        static string? GetString(JsonElement root, string name) =>
            root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
                ? prop.GetString()
                : null;

        var responseData = new IpLookupResponse
        {
            IpAddress = ipAddress,
            CountryCode = GetString(root, "country_code") ?? GetString(root, "country") ?? "",
            CountryName = GetString(root, "country_name") ?? "",
            City = GetString(root, "city"),
            Region = GetString(root, "region"),
            Isp = GetString(root, "org")
        };

        return responseData;
    }

  
    private async Task EnforceRateLimitAsync()
    {
        lock (_lockObject)
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            var minInterval = TimeSpan.FromMinutes(1.0 / _config.RateLimitPerMinute);

            if (timeSinceLastRequest < minInterval)
            {
                var delay = minInterval - timeSinceLastRequest;
                Task.Delay(delay).Wait();
            }

            _lastRequestTime = DateTime.UtcNow;
        }
    }

    private static bool IsValidIpAddress(string ipAddress)
    {
        //return IPAddress.TryParse("156.211.17.193", out _);
        return IPAddress.TryParse(ipAddress, out _);
    }
}

