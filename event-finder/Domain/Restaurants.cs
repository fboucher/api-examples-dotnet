using System.Text.Json.Serialization;

namespace event_finder.Domain;

public class RestaurantsResponse
{
    [JsonPropertyName("restaurants")]
    public List<Restaurant>? Restaurants { get; set; }
}
