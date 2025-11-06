using Microsoft.AspNetCore.Http;

namespace BlockedCountries.Business.Services;

public interface IIpValidationService
{
    string GetClientIpAddress(HttpContext httpContext);
}

