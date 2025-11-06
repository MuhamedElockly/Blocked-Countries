using BlockedCountries.Data.Models;

namespace BlockedCountries.Data.Repositories;

public interface ICountryRepository
{
    Task<bool> AddBlockedCountryAsync(CountryInfo country);
    Task<bool> RemoveBlockedCountryAsync(string countryCode);
    Task<CountryInfo?> GetBlockedCountryAsync(string countryCode);
    Task<IEnumerable<CountryInfo>> GetAllBlockedCountriesAsync();
    Task<bool> IsCountryBlockedAsync(string countryCode);
    Task<bool> AddTemporalBlockAsync(CountryInfo country);
    Task RemoveExpiredTemporalBlocksAsync();
}


