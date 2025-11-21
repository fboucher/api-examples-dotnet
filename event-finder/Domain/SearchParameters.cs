using Reka.SDK;

namespace event_finder.Domain;

public class SearchParameters
{
    public string Topic { get; set; } = string.Empty;
    public UserLocationApproximate UserLocation { get; set; } = new UserLocationApproximate();
    public string AllowedDomains { get; set; } = string.Empty; // comma-separated
    public string BlockedDomains { get; set; } = string.Empty; // comma-separated
}