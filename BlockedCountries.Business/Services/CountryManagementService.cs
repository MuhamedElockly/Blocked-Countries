using Microsoft.Extensions.Logging;
using BlockedCountries.Business.Models.Requests;
using BlockedCountries.Business.Models.Responses;
using BlockedCountries.Data.Models;
using BlockedCountries.Data.Repositories;

namespace BlockedCountries.Business.Services;

public class CountryManagementService : ICountryManagementService
{
    private readonly ICountryRepository _countryRepository;
    private readonly ILogger<CountryManagementService> _logger;

    private static readonly Dictionary<string, string> CountryNames = new()
    {
        { "US", "United States" }, { "GB", "United Kingdom" }, { "EG", "Egypt" },
        { "CA", "Canada" }, { "AU", "Australia" }, { "DE", "Germany" },
        { "FR", "France" }, { "IT", "Italy" }, { "ES", "Spain" },
        { "BR", "Brazil" }, { "IN", "India" }, { "CN", "China" },
        { "JP", "Japan" }, { "KR", "South Korea" }, { "MX", "Mexico" },
        { "RU", "Russia" }, { "TR", "Turkey" }, { "SA", "Saudi Arabia" },
        { "AE", "United Arab Emirates" }, { "ZA", "South Africa" }
    };

    public CountryManagementService(
        ICountryRepository countryRepository,
        ILogger<CountryManagementService> logger)
    {
        _countryRepository = countryRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<BlockedCountryResponse>> BlockCountryAsync(BlockCountryRequest request)
    {
       
        if (string.IsNullOrWhiteSpace(request.CountryCode))
        {
            return ServiceResult<BlockedCountryResponse>.Failure("Country code is required", 400);
        }

        var normalizedCode = request.CountryCode.ToUpperInvariant();
        
        if (!IsValidCountryCode(normalizedCode))
        {
            _logger.LogWarning("Invalid country code: {CountryCode}", request.CountryCode);
            return ServiceResult<BlockedCountryResponse>.Failure("Invalid country code", 400);
        }

        var countryName = GetCountryName(normalizedCode);
        var existingCountry = await _countryRepository.GetBlockedCountryAsync(normalizedCode);
        if (existingCountry != null)
        {
            _logger.LogInformation("Country {CountryCode} is already blocked", normalizedCode);
            return ServiceResult<BlockedCountryResponse>.Success(MapToResponse(existingCountry));
        }

        var countryInfo = new CountryInfo
        {
            CountryCode = normalizedCode,
            CountryName = countryName,
            BlockedAt = DateTime.UtcNow,
            IsTemporalBlock = false
        };

        var added = await _countryRepository.AddBlockedCountryAsync(countryInfo);
        if (!added)
        {
            return ServiceResult<BlockedCountryResponse>.Failure("Failed to block country", 400);
        }

        _logger.LogInformation("Country {CountryCode} has been blocked", normalizedCode);
        return ServiceResult<BlockedCountryResponse>.Success(MapToResponse(countryInfo));
    }

    public async Task<ServiceResult<bool>> UnblockCountryAsync(string countryCode)
    {
        var normalizedCode = countryCode.ToUpperInvariant();
        var removed = await _countryRepository.RemoveBlockedCountryAsync(normalizedCode);
        
        if (removed)
        {
            _logger.LogInformation("Country {CountryCode} has been unblocked", normalizedCode);
            return ServiceResult<bool>.Success(true);
        }
        else
        {
            _logger.LogWarning("Attempted to unblock country {CountryCode} that was not blocked", normalizedCode);
            return ServiceResult<bool>.Failure($"Country {countryCode} is not currently blocked", 404);
        }
    }

    public async Task<ServiceResult<PagedResponse<BlockedCountryResponse>>> GetBlockedCountriesAsync(
        int page, 
        int pageSize, 
        string? searchTerm)
    {
        
        if (page < 1)
            page = 1;
        if (pageSize < 1 || pageSize > 100) 
            pageSize = 10;

        var allCountries = await _countryRepository.GetAllBlockedCountriesAsync();
        var countries = allCountries.ToList();

        // Filter out expired temporal blocks
        countries = countries
            .Where(c => !c.IsTemporalBlock || !c.ExpiresAt.HasValue || c.ExpiresAt.Value > DateTime.UtcNow)
            .ToList();

        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToUpperInvariant();
            countries = countries
                .Where(c => c.CountryCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                           c.CountryName.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Apply pagination
        var totalCount = countries.Count;
        var pagedCountries = countries
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToResponse)
            .ToList();

        return ServiceResult<PagedResponse<BlockedCountryResponse>>.Success(new PagedResponse<BlockedCountryResponse>
        {
            Items = pagedCountries,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ServiceResult<BlockedCountryResponse>> AddTemporalBlockAsync(TemporalBlockRequest request)
    {
        
        if (string.IsNullOrWhiteSpace(request.CountryCode))
        {
            return ServiceResult<BlockedCountryResponse>.Failure("Country code is required", 400);
        }

        if (request.DurationMinutes < 1 || request.DurationMinutes > 1440)
        {
            return ServiceResult<BlockedCountryResponse>.Failure("Duration must be between 1 and 1440 minutes (24 hours)", 400);
        }

        var normalizedCode = request.CountryCode.ToUpperInvariant();

        if (!IsValidCountryCode(normalizedCode))
        {
            _logger.LogWarning("Invalid country code: {CountryCode}", request.CountryCode);
            return ServiceResult<BlockedCountryResponse>.Failure("Invalid country code", 400);
        }

        // Check if country is already blocked
        var existingCountry = await _countryRepository.GetBlockedCountryAsync(normalizedCode);
        if (existingCountry != null)
        {
            _logger.LogWarning("Country {CountryCode} is already blocked", normalizedCode);
            return ServiceResult<BlockedCountryResponse>.Failure($"Country {request.CountryCode} is already blocked", 409);
        }

        var countryName = GetCountryName(normalizedCode);
        var expiresAt = DateTime.UtcNow.AddMinutes(request.DurationMinutes);
        var countryInfo = new CountryInfo
        {
            CountryCode = normalizedCode,
            CountryName = countryName,
            BlockedAt = DateTime.UtcNow,
            IsTemporalBlock = true,
            ExpiresAt = expiresAt
        };

        var added = await _countryRepository.AddTemporalBlockAsync(countryInfo);
        if (!added)
        {
            return ServiceResult<BlockedCountryResponse>.Failure("Failed to add temporal block", 400);
        }

        _logger.LogInformation("Country {CountryCode} has been temporarily blocked for {Duration} minutes", 
            normalizedCode, request.DurationMinutes);
        
        return ServiceResult<BlockedCountryResponse>.Success(MapToResponse(countryInfo));
    }

    public async Task<bool> IsCountryBlockedAsync(string countryCode)
    {
        return await _countryRepository.IsCountryBlockedAsync(countryCode.ToUpperInvariant());
    }

    private static BlockedCountryResponse MapToResponse(CountryInfo country)
    {
        return new BlockedCountryResponse
        {
            CountryCode = country.CountryCode,
            CountryName = country.CountryName,
            BlockedAt = country.BlockedAt,
            IsTemporalBlock = country.IsTemporalBlock,
            ExpiresAt = country.ExpiresAt
        };
    }

    private static bool IsValidCountryCode(string countryCode)
    {
        
        if (string.IsNullOrWhiteSpace(countryCode))
            return false;

        
        if (countryCode.Length != 2)
            return false;

       
        if (!countryCode.All(char.IsLetter))
            return false;

        if (!countryCode.All(char.IsUpper))
            return false;

       
        return true;
    }


    public string GetCountryName(string countryCode)
    {
        return CountryNames.TryGetValue(countryCode.ToUpperInvariant(), out var name) 
            ? name 
            : countryCode;
    }
}

