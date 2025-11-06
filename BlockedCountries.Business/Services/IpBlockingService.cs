using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using BlockedCountries.Business.Models;
using BlockedCountries.Business.Models.Responses;
using BlockedCountries.Data.Models;
using BlockedCountries.Data.Repositories;

namespace BlockedCountries.Business.Services;

public class IpBlockingService : IIpBlockingService
{
    private readonly IGeolocationService _geolocationService;
    private readonly ICountryManagementService _countryManagementService;
    private readonly IBlockedAttemptRepository _attemptRepository;
    private readonly ILogger<IpBlockingService> _logger;

    public IpBlockingService(
        IGeolocationService geolocationService,
        ICountryManagementService countryManagementService,
        IBlockedAttemptRepository attemptRepository,
        ILogger<IpBlockingService> logger)
    {
        _geolocationService = geolocationService;
        _countryManagementService = countryManagementService;
        _attemptRepository = attemptRepository;
      
        _logger = logger;
    }

    public async Task<ServiceResult<IpLookupResponse>> LookupIpAddressAsync(string? ipAddress)
    {
       

        var result = await _geolocationService.LookupIpAddressAsync(ipAddress);

        if (result == null)
        {
            _logger.LogWarning("Could not lookup information for IP address: {IpAddress}", ipAddress);
            return ServiceResult<IpLookupResponse>.Failure($"Could not lookup information for IP address: {ipAddress}", 404);
        }

        return ServiceResult<IpLookupResponse>.Success(result);
    }

    public async Task<ServiceResult<CheckBlockResponse>> CheckIpBlockStatusAsync(string ipAddress,HttpContext httpContext)
    {
        
        var lookupResult = await _geolocationService.LookupIpAddressAsync(ipAddress);

        var countryCode = lookupResult?.CountryCode ?? "Unknown";
        var isBlocked = false;

        if (!string.IsNullOrEmpty(countryCode) && countryCode != "Unknown")
        {
            isBlocked = await _countryManagementService.IsCountryBlockedAsync(countryCode);
        }

        
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        var attempt = new BlockedAttempt
        {
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow,
            CountryCode = countryCode,
            IsBlocked = isBlocked,
            UserAgent = userAgent
        };

        await _attemptRepository.AddAttemptAsync(attempt);

        return ServiceResult<CheckBlockResponse>.Success(new CheckBlockResponse
        {
            IpAddress = ipAddress,
            CountryCode = countryCode,
            IsBlocked = isBlocked
        });
    }
}

