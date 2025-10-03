using System.Text.Json.Serialization;

namespace VideoAnalyzer.Services;

public class RekaVideoUploadResponse
{
    [JsonPropertyName("video_id")]
    public string VideoId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}