using System.Text.Json.Serialization;

namespace event_finder.Domain;

public class EventsResponse
{
    [JsonPropertyName("events")]
    public List<TechEvent>? Events { get; set; }
}
