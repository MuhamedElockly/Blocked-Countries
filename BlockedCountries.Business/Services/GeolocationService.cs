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

                var url = _config.Provider.ToLowerInvariant() switch
                {
                    "ipapi" => $"/{ipAddress}/json/",
                    "ipgeolocation" => $"/ipgeo?apiKey={_config.ApiKey}&ip={ipAddress}",
                    _ => $"/{ipAddress}/json/"
                };

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

                if (_config.Provider.ToLowerInvariant() == "ipapi")
                {
                    return await ParseIpApiResponseAsync(ipAddress, response);
                }
                else
                {
                    return await ParseIpGeolocationResponseAsync(ipAddress, response);
                }
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
        var jsonDoc = JsonDocument.Parse(json);

        if (jsonDoc.RootElement.TryGetProperty("error", out var errorElement))
        {
            if (errorElement.ValueKind == JsonValueKind.True || 
                (errorElement.ValueKind == JsonValueKind.String && errorElement.GetString()?.ToLower() == "true"))
            {
                var reason = jsonDoc.RootElement.TryGetProperty("reason", out var reasonElement) 
                    ? reasonElement.GetString() ?? "Unknown error" 
                    : "Unknown error";
                
                _logger.LogWarning("API returned error for IP {IpAddress}: {Reason}", ipAddress, reason);
                return null;
            }
        }

        string? countryCode = null;
        if (jsonDoc.RootElement.TryGetProperty("country_code", out var cc))
        {
            countryCode = cc.ValueKind == JsonValueKind.String ? cc.GetString() : null;
        }
        else if (jsonDoc.RootElement.TryGetProperty("country", out var country))
        {
            countryCode = country.ValueKind == JsonValueKind.String ? country.GetString() : null;
        }

        string? countryName = null;
        if (jsonDoc.RootElement.TryGetProperty("country_name", out var cn))
        {
            countryName = cn.ValueKind == JsonValueKind.String ? cn.GetString() : null;
        }

        string? city = null;
        if (jsonDoc.RootElement.TryGetProperty("city", out var cityElement))
        {
            city = cityElement.ValueKind == JsonValueKind.String ? cityElement.GetString() : null;
        }

        string? region = null;
        if (jsonDoc.RootElement.TryGetProperty("region", out var regionElement))
        {
            region = regionElement.ValueKind == JsonValueKind.String ? regionElement.GetString() : null;
        }

        string? org = null;
        if (jsonDoc.RootElement.TryGetProperty("org", out var orgElement))
        {
            org = orgElement.ValueKind == JsonValueKind.String ? orgElement.GetString() : null;
        }

        return new IpLookupResponse
        {
            IpAddress = ipAddress,
            CountryCode = countryCode ?? "",
            CountryName = countryName ?? "",
            Isp = org,
            City = city,
            Region = region
        };
    }

    private async Task<IpLookupResponse?> ParseIpGeolocationResponseAsync(string ipAddress, HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(json);

        return new IpLookupResponse
        {
            IpAddress = ipAddress,
            CountryCode = jsonDoc.RootElement.TryGetProperty("country_code2", out var cc) ? cc.GetString() ?? "" : "",
            CountryName = jsonDoc.RootElement.TryGetProperty("country_name", out var cn) ? cn.GetString() ?? "" : "",
            Isp = jsonDoc.RootElement.TryGetProperty("isp", out var isp) ? isp.GetString() : null,
            City = jsonDoc.RootElement.TryGetProperty("city", out var city) ? city.GetString() : null,
            Region = jsonDoc.RootElement.TryGetProperty("state_prov", out var region) ? region.GetString() : null
        };
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
        return IPAddress.TryParse(ipAddress, out _);
    }
}

