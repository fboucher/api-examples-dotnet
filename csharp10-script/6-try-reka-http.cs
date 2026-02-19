
#:package DotNetEnv@3.1.1

using DotNetEnv;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

Env.Load();

var API_KEY = Environment.GetEnvironmentVariable("REKA_API_KEY")!;
var baseUrl = "https://api.reka.ai/v1/chat/completions";
var modelName = "reka-flash-research";

// var API_KEY = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
// var baseUrl = "https://api.openai.com/v1/chat/completions";
// var modelName = "gpt-5";

// var API_KEY = "ollama";
// var baseUrl = "http://192.168.2.11:11434/v1/chat/completions";
// var modelName = "llama3.1:8b";

using var httpClient = new HttpClient();
httpClient.Timeout = Timeout.InfiniteTimeSpan;

var requestPayload = new ChatRequest(
    Model: modelName,
    Messages: new[]
    {
        new ChatMessage(
            Role: "user",
            Content: "Suggest 3 tech event, with a AI focus, that I can attend in the United States")
    });

var jsonPayload = JsonSerializer.Serialize(requestPayload, ChatRequestContext.Default.ChatRequest);

using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl);
request.Headers.Add("Authorization", $"Bearer {API_KEY}");
request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

try
{
    var response = await httpClient.SendAsync(request);

    var responseContent = await response.Content.ReadAsStringAsync();

    if (response.IsSuccessStatusCode)
    {
        var jsonDocument = JsonDocument.Parse(responseContent);

        var contentString = jsonDocument.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        Console.WriteLine(contentString);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}

[JsonSerializable(typeof(ChatRequest))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class ChatRequestContext : JsonSerializerContext
{
}

internal sealed record ChatRequest(string Model, ChatMessage[] Messages);

internal sealed record ChatMessage(string Role, string Content);
