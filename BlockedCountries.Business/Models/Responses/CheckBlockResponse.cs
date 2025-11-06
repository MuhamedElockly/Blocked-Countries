namespace BlockedCountries.Business.Models.Responses;

public class CheckBlockResponse
{
    public string IpAddress { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
}



