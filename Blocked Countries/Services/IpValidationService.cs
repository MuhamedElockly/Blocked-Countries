using BlockedCountries.Api.Middleware;
using BlockedCountries.Business.Services;

namespace BlockedCountries.Api.Services;

public class IpValidationService : IIpValidationService
{
    public string GetClientIpAddress(HttpContext httpContext)
    {
        // Use the extension method which first checks the middleware-stored IP
        return httpContext.GetClientIpAddress();
    }
}
