using System.Text.Json.Serialization;

namespace event_finder.Domain;

public class NominatimResponse
{
    [JsonPropertyName("address")]
    public UserLocation? Address { get; set; }
}

public class UserLocation
{
    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("town")]
    public string? Town { get; set; }

    [JsonPropertyName("village")]
    public string? Village { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }
    
    [JsonPropertyName("region")]
    public string? Region { get; set; }
}

public class Position
{
    public Coordinates? Coordinates { get; set; }
}

public class Coordinates
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}