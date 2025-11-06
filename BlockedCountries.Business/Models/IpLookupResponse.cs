namespace BlockedCountries.Business.Models;

public class IpLookupResponse
{
    public string IpAddress { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string? Isp { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
}


