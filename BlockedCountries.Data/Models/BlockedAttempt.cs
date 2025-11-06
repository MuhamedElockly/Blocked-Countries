namespace BlockedCountries.Data.Models;

public class BlockedAttempt
{
    public string IpAddress { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public string UserAgent { get; set; } = string.Empty;
}



