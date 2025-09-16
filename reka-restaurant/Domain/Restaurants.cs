using System.Text.Json.Serialization;

namespace web.Domain;

public class RestaurantsResponse
{
    [JsonPropertyName("restaurants")]
    public List<Restaurant>? Restaurants { get; set; }
}
