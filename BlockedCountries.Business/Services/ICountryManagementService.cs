using BlockedCountries.Business.Models.Requests;
using BlockedCountries.Business.Models.Responses;

namespace BlockedCountries.Business.Services;

public interface ICountryManagementService
{
    Task<ServiceResult<BlockedCountryResponse>> BlockCountryAsync(BlockCountryRequest request);
    Task<ServiceResult<bool>> UnblockCountryAsync(string countryCode);
    Task<ServiceResult<PagedResponse<BlockedCountryResponse>>> GetBlockedCountriesAsync(int page, int pageSize, string? searchTerm);
    Task<ServiceResult<BlockedCountryResponse>> AddTemporalBlockAsync(TemporalBlockRequest request);
    Task<bool> IsCountryBlockedAsync(string countryCode);
 
}



