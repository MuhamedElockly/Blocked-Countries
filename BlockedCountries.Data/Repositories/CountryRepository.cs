using System.Collections.Concurrent;
using BlockedCountries.Data.Models;

namespace BlockedCountries.Data.Repositories;

public class CountryRepository : ICountryRepository
{
    private readonly ConcurrentDictionary<string, CountryInfo> _blockedCountries = new();

    public Task<bool> AddBlockedCountryAsync(CountryInfo country)
    {
        return Task.FromResult(_blockedCountries.TryAdd(country.CountryCode.ToUpperInvariant(), country));
    }

    public Task<bool> RemoveBlockedCountryAsync(string countryCode)
    {
        return Task.FromResult(_blockedCountries.TryRemove(countryCode.ToUpperInvariant(), out _));
    }

    public Task<CountryInfo?> GetBlockedCountryAsync(string countryCode)
    {
        _blockedCountries.TryGetValue(countryCode.ToUpperInvariant(), out var country);
        return Task.FromResult(country);
    }

    public Task<IEnumerable<CountryInfo>> GetAllBlockedCountriesAsync()
    {
        return Task.FromResult<IEnumerable<CountryInfo>>(_blockedCountries.Values.ToList());
    }

    public Task<bool> IsCountryBlockedAsync(string countryCode)
    {
        var country = GetBlockedCountryAsync(countryCode).Result;
        if (country == null)
            return Task.FromResult(false);

        
        if (country.IsTemporalBlock && country.ExpiresAt.HasValue && country.ExpiresAt.Value < DateTime.UtcNow)
        {
           
            RemoveBlockedCountryAsync(countryCode);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public Task<bool> AddTemporalBlockAsync(CountryInfo country)
    {
        
        if (_blockedCountries.ContainsKey(country.CountryCode.ToUpperInvariant()))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(_blockedCountries.TryAdd(country.CountryCode.ToUpperInvariant(), country));
    }

    public Task RemoveExpiredTemporalBlocksAsync()
    {
        var expiredCountries = _blockedCountries
            .Where(kvp => kvp.Value.IsTemporalBlock && 
                         kvp.Value.ExpiresAt.HasValue && 
                         kvp.Value.ExpiresAt.Value < DateTime.UtcNow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var countryCode in expiredCountries)
        {
            _blockedCountries.TryRemove(countryCode, out _);
        }

        return Task.CompletedTask;
    }
}


