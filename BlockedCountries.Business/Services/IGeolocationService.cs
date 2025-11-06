using BlockedCountries.Business.Models;

namespace BlockedCountries.Business.Services;

public interface IGeolocationService
{
    Task<IpLookupResponse?> LookupIpAddressAsync(string ipAddress);
}


