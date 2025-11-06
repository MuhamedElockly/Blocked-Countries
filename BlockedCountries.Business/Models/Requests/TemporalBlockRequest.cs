namespace BlockedCountries.Business.Models.Requests;

public class TemporalBlockRequest
{
    public string CountryCode { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
}


