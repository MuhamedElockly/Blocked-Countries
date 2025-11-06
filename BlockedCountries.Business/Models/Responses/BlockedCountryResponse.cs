namespace BlockedCountries.Business.Models.Responses;

public class BlockedCountryResponse
{
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public DateTime BlockedAt { get; set; }
    public bool IsTemporalBlock { get; set; }
    public DateTime? ExpiresAt { get; set; }
}



