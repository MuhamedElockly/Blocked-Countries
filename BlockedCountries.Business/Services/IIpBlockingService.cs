using Microsoft.AspNetCore.Http;
using BlockedCountries.Business.Models;
using BlockedCountries.Business.Models.Responses;

namespace BlockedCountries.Business.Services;

public interface IIpBlockingService
{
    Task<ServiceResult<IpLookupResponse>> LookupIpAddressAsync(string? ipAddress, HttpContext httpContext);
    Task<ServiceResult<CheckBlockResponse>> CheckIpBlockStatusAsync(HttpContext httpContext);
}

