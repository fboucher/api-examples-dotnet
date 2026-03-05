using System.Text.Json;
using System.Text.Json.Serialization;

namespace VideoAnalyzer.Services;

public class RekaQAAnswerDto
{
    // chat_response can be a plain string or a JSON object per the Reka API spec
    [JsonPropertyName("chat_response")]
    public JsonElement? ChatResponse { get; set; }
    public string? system_message { get; set; }
    public string? error { get; set; }
    public string status { get; set; } = string.Empty;
    public object? debug_chunks { get; set; }
    public string debug_predicted_start_time { get; set; } = string.Empty;
    public string debug_predicted_end_time { get; set; } = string.Empty;
}