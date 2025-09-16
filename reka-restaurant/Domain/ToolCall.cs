using System.Text.Json.Serialization;

namespace web.Domain;

public class ToolCall
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("args")]
    public object? Args { get; set; }
}
