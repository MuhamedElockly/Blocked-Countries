namespace BlockedCountries.Business.Configuration;

public class GeolocationApiConfig
{
    public const string SectionName = "GeolocationApi";
    
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://ipapi.co";
    public string Provider { get; set; } = "ipapi"; 
    public int RateLimitPerMinute { get; set; } = 60;
}

